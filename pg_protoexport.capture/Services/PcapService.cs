using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Diagnostics;
using System.Text;
namespace pg_protoexport;

public sealed class PcapService(ILogger<PcapService> logger, IOptions<PcapPostgresOptions> pcapPostgresOptions) : IPcapService
{
    public static IPcapService Create(ILoggerFactory? loggerFactory = null, PcapPostgresOptions? options = null)
    {
        options ??= new PcapPostgresOptions().AddDefaultPostgresMessages();

        return new PcapService(loggerFactory.CreateLoggerOrNull<PcapService>(), Microsoft.Extensions.Options.Options.Create(options));
    }

    internal PcapPostgresOptions Options { get; init; } = pcapPostgresOptions.Value;

    public IEnumerable<PostgresPacket> ConvertPcap(string pcapFile, ushort pgsqlPortNumber = 5432)
    {
        PcapReadState state = new()
        {
            Port = pgsqlPortNumber
        };

        using var device = new CaptureFileReaderDevice(pcapFile);
        device.Open(new());

        // Capture packets using GetNextPacket()
        int packetRelativeIndex = 0;
        while (device.GetNextPacket(out PacketCapture e) == GetPacketStatus.PacketRead)
        {
            var time = e.Header.Timeval.Date;
            var len = e.Data.Length;
            var rawPacket = e.GetPacket();

            var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
            if (tcpPacket != null) // only TCP packets
            {
                var ipPacket = (PacketDotNet.IPPacket)tcpPacket.ParentPacket;
                System.Net.IPAddress srcIp = ipPacket.SourceAddress;
                System.Net.IPAddress dstIp = ipPacket.DestinationAddress;
                int srcPort = tcpPacket.SourcePort;
                int dstPort = tcpPacket.DestinationPort;

                if ((srcPort == pgsqlPortNumber || dstPort == pgsqlPortNumber) && tcpPacket.PayloadData.Length > 0) // only TCP packets on Postgres port with data
                {
                    logger.LogInformation("{Hour}:{Min}:{Sec},{Ms} Len={Len} {SrcIp}:{SrcPort} -> {DstIp}:{DstPort}",
                        time.Hour, time.Minute, time.Second, time.Millisecond, len,
                        srcIp, srcPort, dstIp, dstPort);

                    var pgPacket = ParsePacket(tcpPacket, ipPacket, state);
                    pgPacket.Timestamp = time;
                    pgPacket.PacketIndex = packetRelativeIndex++;
                    yield return pgPacket;

                }
            }
        }
    }

    private PostgresPacket ParsePacket(PacketDotNet.TcpPacket tcpPacket, PacketDotNet.IPPacket ipPacket, PcapReadState state)
    {
        var pgPacket = new PostgresPacket
        {
            // IPV6
            SourceAddress = ipPacket.SourceAddress,
            DestinationAddress = ipPacket.DestinationAddress,

            // TCP
            SourcePort = tcpPacket.SourcePort,
            DestinationPort = tcpPacket.DestinationPort,
            IsFrontEnd = tcpPacket.DestinationPort == state.Port
        };

        // Reconstruct packet
        pgPacket.PayloadData = state.PreviousBufferLeftover is null ? tcpPacket.PayloadData
                                                                : [.. state.PreviousBufferLeftover, .. tcpPacket.PayloadData];

        using var memStream = new MemoryStream(pgPacket.PayloadData);
        using var binaryReader = new BinaryReader(memStream, Encoding.UTF8);
        using var reader = new PcapBinaryReader(binaryReader, Encoding.UTF8);
        var parserInfo = new ParserInfo(reader, state);

        byte[]? nextRemainder = null;
        while (binaryReader.BaseStream.Position != memStream.Length)
        {
            var currentPosition = binaryReader.BaseStream.Position;
            ushort clientPort = pgPacket.IsFrontEnd ? pgPacket.SourcePort : pgPacket.DestinationPort;
            parserInfo.IsFrontEnd = pgPacket.IsFrontEnd;
            parserInfo.ClientPort = clientPort;
            if (TryReadMessage(parserInfo, out var message))
            {
                Debug.Assert(message != null);
                pgPacket.Messages.Add(message!);
            }
            else
            {
                binaryReader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
                nextRemainder = reader.ReadBytes((int)(memStream.Length - currentPosition));
            }
        }

        state.PreviousBufferLeftover = nextRemainder;

        return pgPacket;
    }

    private bool TryReadMessage(ParserInfo info, out PostgresMessageBase? message)
    {
        if (Options.RecordFieldMetadata)
            info.Reader.BeginMessage();
        try
        {
            message = null;
            char messageCode;
            using (info.Reader.BeginField("code"))
                messageCode = info.Reader.ReadChar();

            // Frontend non-TLV startup-phase frames carry no 1-byte code: the first byte is
            // the high byte of the int32 length and is always 0 (length < 2^24).
            int messageLength = 0;
            if (messageCode == 0)
            {
                messageCode = '?';
                info.Reader.Seek(-1, SeekOrigin.Current);
                using (info.Reader.BeginField("length"))
                    messageLength = info.Reader.ReadInt32();
            }

            // Backend single-byte reply to a prior SSL/GSSENC probe on the same port. When it
            // matches we're done; otherwise the probe state is cleared and we fall through.
            if (!info.IsFrontEnd && TryReadProbeResponse(info, messageCode, out message))
                return true;

            var pgMessageRaw = Options.MessageCatalog.GetMessage(messageCode, info.IsFrontEnd);
            if (pgMessageRaw == null)
            {
                logger.LogWarning("Unknown message with code: {MessageCode}", messageCode);
                message = UnknownMessage.Read(new PostgresMessageDescriptor(messageCode, "Unknown", info.IsFrontEnd), info.Reader);
                RecordMetadata(message, info.Reader);
                return true;
            }
            var pgMessage = pgMessageRaw.Value with { IsFrontEnd = info.IsFrontEnd };

            // Check if buffer empty
            if (!info.Reader.HasSufficientData(sizeof(int))) // code + length
                return false;

            message = ReadKnownMessage(pgMessage, info, messageLength, messageCode);

            if (message == null) // Cannot read, insufficient buffer data available
                return false;

            ApplyPostDispatchState(message, info);
            RecordMetadata(message, info.Reader);

            return true;
        }
        catch (EndOfStreamException)
        {
            message = null;
            return false;
        }
    }

    // Backend single-byte reply to a prior SSL/GSSENC probe on the same port. The byte was
    // already consumed as `messageCode`. Returns true (with `message` set) when it is a valid
    // probe reply; otherwise clears the stuck probe state and returns false so the caller falls
    // through to normal catalog dispatch.
    private bool TryReadProbeResponse(ParserInfo info, char messageCode, out PostgresMessageBase? message)
    {
        message = null;
        var probe = info.State.LastStartupProbe(info.ClientPort);
        if (probe == StartupProbeKind.None)
            return false;

        info.State.SetLastStartupProbe(info.ClientPort, StartupProbeKind.None);
        if (TryReadStartupProbeResponse(probe, messageCode, info.IsFrontEnd, out message))
        {
            RecordMetadata(message!, info.Reader);
            return true;
        }

        // The byte wasn't a valid probe response — capture lost the reply.
        logger.LogWarning("Expected SSL/GSSENC probe response but saw code '{MessageCode}' — clearing probe state", messageCode);
        return false;
    }

    // Dispatch a known catalog message to its typed reader. External message types are handled
    // through the documented CustomMessageProcessor hook (falling back to UnknownMessage), so this
    // closed set is the only place that grows when a new built-in wire message is supported.
    private PostgresMessageBase? ReadKnownMessage(PostgresMessageDescriptor pgMessage, ParserInfo info, int messageLength, char messageCode) =>
        pgMessage.Name switch
        {
            "Parse" => ParseMessage.Read(pgMessage, info.Reader),
            "Bind" => BindMessage.Read(pgMessage, info.Reader),
            "Describe" => DescribeMessage.Read(pgMessage, info.Reader),
            "Execute" => ExecuteMessage.Read(pgMessage, info.Reader),
            "Sync" => SyncMessage.Read(pgMessage, info.Reader),
            "Query" => QueryMessage.Read(pgMessage, info.Reader),
            "NoData" => NoDataMessage.Read(pgMessage, info.Reader),
            "BindComplete" => BindCompleteMessage.Read(pgMessage, info.Reader),
            "ParseComplete" => ParseCompleteMessage.Read(pgMessage, info.Reader),
            "ParameterDescription" => ParameterDescriptionMessage.Read(pgMessage, info.Reader),
            "RowDescription" => RowDescriptionMessage.Read(pgMessage, info.Reader),
            "ReadyForQuery" => ReadyForQueryMessage.Read(pgMessage, info.Reader),
            "DataRow" => DataRowMessage.Read(pgMessage, info.Reader, info.State.LastRowDescription),
            "CommandComplete" => CommandCompleteMessage.Read(pgMessage, info.Reader),
            "NoticeResponse" => NoticeResponseMessage.Read(pgMessage, info.Reader),
            "Terminate" => TerminateMessage.Read(pgMessage, info.Reader),
            "StartupMessage" => DispatchStartupPhaseFrontend(pgMessage, messageLength, info.Reader),
            "AuthenticationRequest" => AuthenticationMessage.Read(pgMessage, info.Reader),
            "Password" => info.State.GetLastAuthPacket(info.ClientPort)!.ReadResponseMessage(pgMessage, info.Reader),
            "ParameterStatus" => ParameterStatusMessage.Read(pgMessage, info.Reader),
            "BackendKeyData" => BackendKeyDataMessage.Read(pgMessage, info.Reader),
            "ErrorResponse" => ErrorResponseMessage.Read(pgMessage, info.Reader),
            "CopyInResponse" => CopyInResponseMessage.Read(pgMessage, info.Reader),
            "CopyOutResponse" => CopyOutResponseMessage.Read(pgMessage, info.Reader),
            "CopyBothResponse" => CopyBothResponseMessage.Read(pgMessage, info.Reader),
            "CopyData" => CopyDataMessage.Read(pgMessage, info.Reader, info.State.BeginCopyDataChunk(info.ClientPort)),
            "CopyDone" => CopyDoneMessage.Read(pgMessage, info.Reader),
            "CopyFail" => CopyFailMessage.Read(pgMessage, info.Reader),
            _ => Options.CustomMessageProcessor?.Invoke(pgMessage, info) ?? UnknownMessage.Read(new PostgresMessageDescriptor(messageCode, "Unknown", info.IsFrontEnd), info.Reader)
        };

    // Update the per-connection parser state from an observed message: cache the last auth/row
    // descriptor, correlate cancel keys, track COPY-stream entry/exit, and arm SSL/GSSENC probes.
    private static void ApplyPostDispatchState(PostgresMessageBase message, ParserInfo info)
    {
        if (message is AuthenticationGenericMessage authMsg)
            info.State.SetLastAuthPacket(info.ClientPort, authMsg);
        if (message is RowDescriptionMessage rowDesc)
            info.State.LastRowDescription = rowDesc;
        if (message is BackendKeyDataMessage bkd)
            info.State.RegisterCancelKey(bkd.ProcessId, bkd.SecretKey, info.ClientPort);
        if (message is CancelRequestMessage cancel)
            cancel.CorrelatedClientPort = info.State.LookupCancelTargetClientPort(cancel.ProcessId, (uint)cancel.SecretKey);

        switch (message)
        {
            case CopyInResponseMessage cir:
                info.State.EnterCopyStream(info.ClientPort, CopyStreamKind.In, cir.OverallFormat == 1 ? CopyStreamFormat.Binary : CopyStreamFormat.Text);
                break;
            case CopyOutResponseMessage cor:
                info.State.EnterCopyStream(info.ClientPort, CopyStreamKind.Out, cor.OverallFormat == 1 ? CopyStreamFormat.Binary : CopyStreamFormat.Text);
                break;
            case CopyBothResponseMessage cbr:
                info.State.EnterCopyStream(info.ClientPort, CopyStreamKind.Both, cbr.OverallFormat == 1 ? CopyStreamFormat.Binary : CopyStreamFormat.Text);
                break;
            case CopyDoneMessage:
            case CopyFailMessage:
            case ErrorResponseMessage:
                info.State.ExitCopyStream(info.ClientPort);
                break;
        }

        var probeKind = message switch
        {
            SSLRequestMessage => StartupProbeKind.SSL,
            GSSENCRequestMessage => StartupProbeKind.GSSENC,
            _ => (StartupProbeKind?)null
        };
        if (probeKind.HasValue)
            info.State.SetLastStartupProbe(info.ClientPort, probeKind.Value);
    }

    // Stamp per-field offset/length metadata onto the message. No-op unless RecordFieldMetadata
    // is enabled (off by default to keep exporters byte-identical and avoid per-field allocations).
    private void RecordMetadata(PostgresMessageBase message, PcapBinaryReader reader)
    {
        if (!Options.RecordFieldMetadata)
            return;
        message.PayloadOffset = reader.MessageStartOffset;
        message.OnWireLength = reader.CurrentStreamOffset - reader.MessageStartOffset;
        message.ParsedFields = reader.EndMessage();
    }

    private PostgresMessageBase DispatchStartupPhaseFrontend(PostgresMessageDescriptor pgMessage, int messageLength, PcapBinaryReader reader)
    {
        int requestCode;
        using (reader.BeginField("requestCode")) requestCode = reader.ReadInt32();

        return requestCode switch
        {
            SSLRequestMessage.MagicCode => SSLRequestMessage.Read(pgMessage with { Name = "SSLRequest" }, messageLength, requestCode),
            GSSENCRequestMessage.MagicCode => GSSENCRequestMessage.Read(pgMessage with { Name = "GSSENCRequest" }, messageLength, requestCode),
            CancelRequestMessage.MagicCode => CancelRequestMessage.Read(pgMessage with { Name = "CancelRequest" }, messageLength, requestCode, reader),
            _ when messageLength > 8 => StartupMessageMessage.Read(pgMessage, messageLength, requestCode, reader),
            _ => ReadUnknownStartupPhase(pgMessage, messageLength, requestCode, reader),
        };
    }

    private PostgresMessageBase ReadUnknownStartupPhase(PostgresMessageDescriptor pgMessage, int messageLength, int requestCode, PcapBinaryReader reader)
    {
        logger.LogWarning("Unknown startup-phase request code: {Code:X8} (length {Length})", requestCode, messageLength);
        int remaining = messageLength - 8; // already consumed length(4) + requestCode(4)
        byte[] data;
        using (reader.BeginField("data"))
            data = remaining > 0 ? reader.ReadBytes(remaining) : Array.Empty<byte>();
        return new UnknownMessage(pgMessage with { Name = "UnknownStartupPhase" }, messageLength) { Data = data };
    }

    private static bool TryReadStartupProbeResponse(StartupProbeKind probe, char responseByte, bool isFrontEnd, out PostgresMessageBase? message)
    {
        switch (probe)
        {
            case StartupProbeKind.SSL when responseByte is 'S' or 'N':
                message = SSLResponseMessage.Read(new PostgresMessageDescriptor('?', "SSLResponse", isFrontEnd), accepted: responseByte == 'S');
                return true;
            case StartupProbeKind.GSSENC when responseByte is 'G' or 'N':
                message = GSSENCResponseMessage.Read(new PostgresMessageDescriptor('?', "GSSENCResponse", isFrontEnd), accepted: responseByte == 'G');
                return true;
            default:
                message = null;
                return false;
        }
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace pg_protoexport.tests;

public class StartupPhaseMessageTests
{
    private static PcapService NewService()
    {
        var options = new PcapPostgresOptions();
        options.AddDefaultPostgresMessages();
        return new PcapService(NullLogger<PcapService>.Instance, Options.Create(options));
    }

    [Fact]
    public void SSLRequestMessage_Read_AssignsPayloadFromMagicCode()
    {
        var descriptor = new PostgresMessageDescriptor('?', "SSLRequest", IsFrontEnd: true);
        var msg = SSLRequestMessage.Read(descriptor, messageLength: 8, requestCode: 80877103);

        Assert.Equal(80877103, msg.Payload);
        Assert.Equal(8, msg.Length);
        Assert.Equal("SSLRequest", msg.Name);
    }

    [Fact]
    public void GSSENCRequestMessage_Read_AssignsPayloadFromMagicCode()
    {
        var descriptor = new PostgresMessageDescriptor('?', "GSSENCRequest", IsFrontEnd: true);
        var msg = GSSENCRequestMessage.Read(descriptor, messageLength: 8, requestCode: 80877104);

        Assert.Equal(80877104, msg.Payload);
        Assert.Equal(8, msg.Length);
        Assert.Equal("GSSENCRequest", msg.Name);
    }

    [Fact]
    public void CancelRequestMessage_Read_ParsesPidAndSecret()
    {
        // 8 bytes payload after length+code: pid=12345 (int32 BE) + secret=0xCAFEBABE (int32 BE)
        var bytes = new byte[]
        {
            0x00, 0x00, 0x30, 0x39, // pid 12345
            0xCA, 0xFE, 0xBA, 0xBE  // secret
        };
        using var mem = new MemoryStream(bytes);
        using var binaryReader = new BinaryReader(mem, System.Text.Encoding.UTF8);
        using var reader = new PcapBinaryReader(binaryReader, System.Text.Encoding.UTF8);

        var descriptor = new PostgresMessageDescriptor('?', "CancelRequest", IsFrontEnd: true);
        var msg = CancelRequestMessage.Read(descriptor, messageLength: 16, requestCode: 80877102, reader);

        Assert.Equal(80877102, msg.RequestCode);
        Assert.Equal(12345, msg.ProcessId);
        unchecked { Assert.Equal((int)0xCAFEBABE, msg.SecretKey); }
        Assert.Equal("CancelRequest", msg.Name);
    }

    [Fact]
    public void SSLResponseMessage_Read_AcceptedFlag()
    {
        var descriptor = new PostgresMessageDescriptor('?', "SSLResponse", IsFrontEnd: false);

        var accepted = SSLResponseMessage.Read(descriptor, accepted: true);
        var rejected = SSLResponseMessage.Read(descriptor, accepted: false);

        Assert.True(accepted.Accepted);
        Assert.False(rejected.Accepted);
    }

    [Fact]
    public void GSSENCResponseMessage_Read_AcceptedFlag()
    {
        var descriptor = new PostgresMessageDescriptor('?', "GSSENCResponse", IsFrontEnd: false);

        var accepted = GSSENCResponseMessage.Read(descriptor, accepted: true);
        var rejected = GSSENCResponseMessage.Read(descriptor, accepted: false);

        Assert.True(accepted.Accepted);
        Assert.False(rejected.Accepted);
    }

    [Fact]
    public void StartupMessageMessage_Read_SplitsProtocolVersionFromInt32()
    {
        // Empty params: just the trailing null byte.
        // Length = 4 (length itself) + 4 (version) + 1 (trailing null) = 9
        using var mem = new MemoryStream(new byte[] { 0x00 });
        using var binaryReader = new BinaryReader(mem, System.Text.Encoding.UTF8);
        using var reader = new PcapBinaryReader(binaryReader, System.Text.Encoding.UTF8);

        var descriptor = new PostgresMessageDescriptor('?', "StartupMessage", IsFrontEnd: true);
        // protocol version 3.0 = 196608 = 0x00030000
        var msg = StartupMessageMessage.Read(descriptor, length: 9, protocolVersion: 196608, reader);

        Assert.Equal(3, msg.ProtocolMajorVersion);
        Assert.Equal(0, msg.ProtocolMinorVersion);
        Assert.Empty(msg.Parameters);
    }

    [Fact]
    public void PcapService_PagilaStartup_FirstFrontendMessageIsProbeNotStartupMessage()
    {
        // pagila-00 captures the startup handshake. Npgsql 10 sends a non-TLS startup probe
        // (GSSENCRequest, code 80877104) as the very first frontend message even with
        // SslMode=Disable. The new dispatch must surface this as GSSENCRequestMessage,
        // not as the legacy "8-byte = always SSLRequest" misclassification.
        var service = NewService();
        var packets = service.ConvertPcap("../../../../docs/examples/captures/pagila-00-startup-handshake-startup-authentication-parameterstatus-readyforquery.pcapng", pgsqlPortNumber: 5434).ToList();

        Assert.NotEmpty(packets);

        var firstFrontendMessage = packets
            .SelectMany(p => p.Messages.Select(m => (packet: p, msg: m)))
            .First(x => x.packet.IsFrontEnd).msg;

        Assert.True(firstFrontendMessage is SSLRequestMessage or GSSENCRequestMessage,
            $"Expected SSLRequestMessage or GSSENCRequestMessage as first frontend message, got {firstFrontendMessage.GetType().Name}");

        // The real handshake includes a StartupMessage too. Confirm it parses as a
        // StartupMessageMessage (not collapsed under the probe type).
        var startup = packets
            .SelectMany(p => p.Messages)
            .OfType<StartupMessageMessage>()
            .FirstOrDefault();
        Assert.NotNull(startup);
        Assert.Equal(3, startup!.ProtocolMajorVersion);
    }

    [Fact]
    public void PcapService_PagilaStartup_ProbeReplyDispatchesToResponseMessage()
    {
        // After the frontend probe, the server replies with a single byte ('N' for reject in
        // the pagila case since the local server has neither TLS nor GSSAPI configured).
        // That single byte must come back as SSLResponseMessage or GSSENCResponseMessage, NOT
        // as NoticeResponseMessage (which the byte's 'N' code would map to under the catalog).
        var service = NewService();
        var packets = service.ConvertPcap("../../../../docs/examples/captures/pagila-00-startup-handshake-startup-authentication-parameterstatus-readyforquery.pcapng", pgsqlPortNumber: 5434).ToList();

        var probeReply = packets
            .SelectMany(p => p.Messages)
            .FirstOrDefault(m => m is SSLResponseMessage or GSSENCResponseMessage);

        Assert.NotNull(probeReply);
    }

    [Fact]
    public void PcapService_PagilaCancelRequest_ProducesCancelRequestMessage()
    {
        // pagila-13 sends a CancelRequest. Before the dispatch rewrite this crashed
        // StartupMessageMessage.Read with a duplicate-key dict error; it must now parse
        // as a CancelRequestMessage with the real pid + secret.
        var service = NewService();
        var packets = service.ConvertPcap("../../../../docs/examples/captures/pagila-13-cancelrequest.pcapng", pgsqlPortNumber: 5434).ToList();

        var cancel = packets
            .SelectMany(p => p.Messages)
            .OfType<CancelRequestMessage>()
            .FirstOrDefault();

        Assert.NotNull(cancel);
        Assert.Equal(80877102, cancel!.RequestCode);
        Assert.NotEqual(0, cancel.ProcessId);
        Assert.NotEqual(0, cancel.SecretKey);
    }
}

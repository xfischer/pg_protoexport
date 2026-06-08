namespace pg_protoexport;

/// <summary>
/// One chunk of COPY payload. Goes in both directions: backend → frontend during COPY ... TO,
/// frontend → backend during COPY ... FROM. The descriptor's <see cref="PostgresMessageDescriptor.IsFrontEnd"/>
/// carries direction.
///
/// To keep memory + export sizes reasonable for replication captures (which can carry MB-sized
/// CopyData frames), only the first <see cref="PreviewMaxBytes"/> bytes of payload are retained
/// in <see cref="PreviewBytes"/>. <see cref="DataLength"/> always reports the on-the-wire payload size.
///
/// When the parser knows the chunk belongs to a binary CopyIn/CopyOut stream and this is the
/// first chunk, <see cref="BinaryHeader"/> is populated with the parsed magic / flags / extension
/// per the PostgreSQL 18 binary-COPY format (<see href="https://www.postgresql.org/docs/18/sql-copy.html"/>).
/// <see cref="IsTrailer"/> flags the 2-byte <c>FF FF</c> end-of-stream marker.
/// </summary>
public class CopyDataMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public const int PreviewMaxBytes = 64;

    /// <summary>Bytes of payload after the 4-byte length header.</summary>
    public int DataLength { get; init; }

    /// <summary>First min(<see cref="DataLength"/>, <see cref="PreviewMaxBytes"/>) bytes of the payload.</summary>
    public byte[] PreviewBytes { get; init; } = [];

    /// <summary>
    /// True if the parser knew this chunk was part of a binary COPY stream; false if text;
    /// null if the COPY-format state was not tracked (e.g. capture started mid-stream).
    /// </summary>
    public bool? IsBinaryFormat { get; init; }

    /// <summary>True when this is the first chunk of a binary CopyIn/CopyOut stream and the file
    /// header was parsed into <see cref="BinaryHeader"/>.</summary>
    public bool IsHeader { get; init; }

    /// <summary>True when the payload is exactly 2 bytes <c>FF FF</c> and we are inside a binary
    /// COPY stream — PostgreSQL's binary trailer marker.</summary>
    public bool IsTrailer { get; init; }

    /// <summary>Parsed binary file header. Non-null iff <see cref="IsHeader"/> is true.</summary>
    public CopyBinaryHeader? BinaryHeader { get; init; }

    internal static CopyDataMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
        => Read(pgMessage, reader, copyStream: null);

    internal static CopyDataMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader, CopyStreamState? copyStream)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        int dataLen = len - 4;

        bool? isBinary = copyStream is null
            ? (bool?)null
            : copyStream.Format == CopyStreamFormat.Binary;

        // Only attempt the binary file-header parse on the first chunk of a CopyIn/CopyOut
        // binary stream. CopyBothResponse carries the streaming-replication sub-protocol,
        // not a binary-COPY file stream — skip the magic check there.
        bool tryHeader = isBinary == true
            && copyStream is { ChunkCount: 0 }
            && (copyStream.Kind == CopyStreamKind.In || copyStream.Kind == CopyStreamKind.Out)
            && dataLen >= 19;

        CopyBinaryHeader? header = null;
        byte[] payload;

        if (tryHeader)
        {
            (header, payload) = ReadBinaryHeader(reader, dataLen);
        }
        else
        {
            using (reader.BeginField("data"))
                payload = dataLen > 0 ? reader.ReadBytes(dataLen) : [];
        }

        byte[] preview = payload.Length > PreviewMaxBytes
            ? payload[..PreviewMaxBytes]
            : payload;

        bool isTrailer = isBinary == true
            && dataLen == 2
            && payload.Length == 2
            && payload[0] == 0xFF
            && payload[1] == 0xFF;

        return new CopyDataMessage(pgMessage, len)
        {
            DataLength = dataLen,
            PreviewBytes = preview,
            IsBinaryFormat = isBinary,
            IsHeader = header != null,
            IsTrailer = isTrailer,
            BinaryHeader = header
        };
    }

    private static (CopyBinaryHeader Header, byte[] Payload) ReadBinaryHeader(PcapBinaryReader reader, int dataLen)
    {
        byte[] sig;
        using (reader.BeginField("signature")) sig = reader.ReadBytes(11);
        bool valid = IsValidMagic(sig);
        uint flags;
        using (reader.BeginField("flags")) flags = (uint)reader.ReadInt32();
        int extLen;
        using (reader.BeginField("headerExtensionLength")) extLen = reader.ReadInt32();

        byte[] extPreview = [];
        int extConsumed = 0;
        if (extLen > 0)
        {
            int takeable = Math.Min(extLen, dataLen - 19);
            if (takeable > 0)
            {
                using (reader.BeginField("headerExtension")) extPreview = reader.ReadBytes(takeable);
                extConsumed = takeable;
            }
        }

        int tupleBytes = dataLen - 19 - extConsumed;
        byte[] tupleData = [];
        if (tupleBytes > 0)
        {
            using (reader.BeginField("tupleData")) tupleData = reader.ReadBytes(tupleBytes);
        }

        var payload = new byte[dataLen];
        Buffer.BlockCopy(sig, 0, payload, 0, 11);
        payload[11] = (byte)(flags >> 24);
        payload[12] = (byte)(flags >> 16);
        payload[13] = (byte)(flags >> 8);
        payload[14] = (byte)flags;
        payload[15] = (byte)(extLen >> 24);
        payload[16] = (byte)(extLen >> 16);
        payload[17] = (byte)(extLen >> 8);
        payload[18] = (byte)extLen;
        if (extConsumed > 0) Buffer.BlockCopy(extPreview, 0, payload, 19, extConsumed);
        if (tupleBytes > 0) Buffer.BlockCopy(tupleData, 0, payload, 19 + extConsumed, tupleBytes);

        var headerExtPreview = extConsumed > 0 ? extPreview : null;
        return (new CopyBinaryHeader(sig, valid, flags, extLen, headerExtPreview), payload);
    }

    private static bool IsValidMagic(ReadOnlySpan<byte> sig) =>
        sig.Length == 11
        && sig[0] == 0x50 && sig[1] == 0x47 && sig[2] == 0x43 && sig[3] == 0x4F
        && sig[4] == 0x50 && sig[5] == 0x59 && sig[6] == 0x0A && sig[7] == 0xFF
        && sig[8] == 0x0D && sig[9] == 0x0A && sig[10] == 0x00;

    public override string GetStringRepresentation()
    {
        if (IsHeader && BinaryHeader is { } h)
            return $"{DataLength} byte(s) [binary-header, magic {(h.SignatureValid ? "OK" : "MISMATCH")}, flags=0x{h.Flags:X8}, ext={h.HeaderExtensionLength}]";
        if (IsTrailer)
            return $"{DataLength} byte(s) [binary-trailer]";
        if (IsBinaryFormat == true)
            return $"{DataLength} byte(s) [binary]";
        return $"{DataLength} byte(s)";
    }
}

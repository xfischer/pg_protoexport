using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace pg_protoexport.tests;

public class CopyMessageTests
{
    private static PcapService NewService()
    {
        var options = new PcapPostgresOptions();
        options.AddDefaultPostgresMessages();
        return new PcapService(NullLogger<PcapService>.Instance, Options.Create(options));
    }

    private static PcapBinaryReader ReaderFor(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
        var br = new BinaryReader(ms, Encoding.UTF8);
        return new PcapBinaryReader(br, Encoding.UTF8);
    }

    [Fact]
    public void CopyInResponseMessage_Read_ParsesFormatAndColumns()
    {
        // length(4) + format(1) + columnCount(2) + 3 * format(2) = 13 bytes total payload
        // (length field itself includes self, so length = 4 + 1 + 2 + 6 = 13)
        var bytes = new byte[]
        {
            0x00, 0x00, 0x00, 0x0D, // length = 13
            0x01,                   // overall format = binary
            0x00, 0x03,             // 3 columns
            0x00, 0x01,             // col 0: binary
            0x00, 0x00,             // col 1: text
            0x00, 0x01              // col 2: binary
        };
        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('G', "CopyInResponse", IsFrontEnd: false);

        var msg = CopyInResponseMessage.Read(descriptor, reader);

        Assert.Equal(13, msg.Length);
        Assert.Equal(1, msg.OverallFormat);
        Assert.Equal(new short[] { 1, 0, 1 }, msg.ColumnFormats);
        Assert.Equal("CopyInResponse", msg.Name);
    }

    [Fact]
    public void CopyOutResponseMessage_Read_ParsesFormatAndColumns()
    {
        var bytes = new byte[]
        {
            0x00, 0x00, 0x00, 0x09, // length = 9
            0x00,                   // overall format = text
            0x00, 0x01,             // 1 column
            0x00, 0x00              // col 0: text
        };
        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('H', "CopyOutResponse", IsFrontEnd: false);

        var msg = CopyOutResponseMessage.Read(descriptor, reader);

        Assert.Equal(0, msg.OverallFormat);
        Assert.Single(msg.ColumnFormats);
        Assert.Equal(0, msg.ColumnFormats[0]);
    }

    [Fact]
    public void CopyBothResponseMessage_Read_ParsesFormatAndColumns()
    {
        var bytes = new byte[]
        {
            0x00, 0x00, 0x00, 0x07, // length = 7
            0x01,                   // overall format = binary
            0x00, 0x00              // 0 columns
        };
        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('W', "CopyBothResponse", IsFrontEnd: false);

        var msg = CopyBothResponseMessage.Read(descriptor, reader);

        Assert.Equal(1, msg.OverallFormat);
        Assert.Empty(msg.ColumnFormats);
    }

    [Fact]
    public void CopyDataMessage_Read_KeepsExactPayload_WhenUnder64Bytes()
    {
        var payload = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        var bytes = new byte[4 + payload.Length];
        // length = 4 (length field itself) + 16 (payload) = 20
        bytes[0] = 0x00; bytes[1] = 0x00; bytes[2] = 0x00; bytes[3] = 0x14;
        Array.Copy(payload, 0, bytes, 4, payload.Length);
        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('d', "CopyData", IsFrontEnd: false);

        var msg = CopyDataMessage.Read(descriptor, reader);

        Assert.Equal(16, msg.DataLength);
        Assert.Equal(payload, msg.PreviewBytes);
    }

    [Fact]
    public void CopyDataMessage_Read_TruncatesPreviewTo64Bytes()
    {
        // 256-byte payload — preview should be exactly the first 64 bytes.
        var payload = Enumerable.Range(0, 256).Select(i => (byte)(i & 0xFF)).ToArray();
        var bytes = new byte[4 + payload.Length];
        // length = 4 + 256 = 260
        bytes[0] = 0x00; bytes[1] = 0x00; bytes[2] = 0x01; bytes[3] = 0x04;
        Array.Copy(payload, 0, bytes, 4, payload.Length);
        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('d', "CopyData", IsFrontEnd: true);

        var msg = CopyDataMessage.Read(descriptor, reader);

        Assert.Equal(256, msg.DataLength);
        Assert.Equal(CopyDataMessage.PreviewMaxBytes, msg.PreviewBytes.Length);
        Assert.Equal(payload[..CopyDataMessage.PreviewMaxBytes], msg.PreviewBytes);
    }

    [Fact]
    public void CopyDoneMessage_Read_HasOnlyLength()
    {
        var bytes = new byte[] { 0x00, 0x00, 0x00, 0x04 }; // length = 4 (header-only)
        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('c', "CopyDone", IsFrontEnd: false);

        var msg = CopyDoneMessage.Read(descriptor, reader);

        Assert.Equal(4, msg.Length);
        Assert.Equal("CopyDone", msg.Name);
    }

    [Fact]
    public void CopyFailMessage_Read_ParsesErrorMessage()
    {
        // length(4) + "boom\0" (5 bytes) = 9
        var errorBytes = Encoding.UTF8.GetBytes("boom\0");
        var bytes = new byte[4 + errorBytes.Length];
        bytes[0] = 0x00; bytes[1] = 0x00; bytes[2] = 0x00; bytes[3] = 0x09;
        Array.Copy(errorBytes, 0, bytes, 4, errorBytes.Length);
        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('f', "CopyFail", IsFrontEnd: true);

        var msg = CopyFailMessage.Read(descriptor, reader);

        Assert.Equal("boom", msg.ErrorMessage);
    }

    [Fact]
    public void PcapService_PagilaCopyOut_ContainsCopyOutResponse_CopyData_CopyDone()
    {
        // pagila-11 streams data S→C: server emits CopyOutResponse, then a series of
        // CopyData frames carrying the binary-format rows, then CopyDone.
        var service = NewService();
        var packets = service.ConvertPcap(
            "../../../../docs/examples/captures/pagila-11-copy-out-binary.pcapng",
            pgsqlPortNumber: 5434).ToList();

        var messages = packets.SelectMany(p => p.Messages).ToList();

        Assert.Contains(messages, m => m is CopyOutResponseMessage);
        Assert.Contains(messages, m => m is CopyDataMessage);
        Assert.Contains(messages, m => m is CopyDoneMessage);

        var resp = messages.OfType<CopyOutResponseMessage>().First();
        Assert.Equal(1, resp.OverallFormat); // binary
        Assert.True(resp.ColumnFormats.Count > 0);

        // First CopyData should carry the binary file header.
        var firstData = messages.OfType<CopyDataMessage>().First();
        Assert.True(firstData.IsHeader);
        Assert.NotNull(firstData.BinaryHeader);
        Assert.True(firstData.BinaryHeader!.SignatureValid);
        Assert.Equal(0u, firstData.BinaryHeader.Flags);
        Assert.Equal(0, firstData.BinaryHeader.HeaderExtensionLength);
        // All subsequent CopyData chunks are binary tuples (or the trailer), never header.
        foreach (var cd in messages.OfType<CopyDataMessage>().Skip(1))
        {
            Assert.False(cd.IsHeader);
            Assert.True(cd.IsBinaryFormat == true);
        }
    }

    [Fact]
    public void PcapService_PagilaCopyIn_ContainsCopyInResponse_CopyData_CopyDone()
    {
        // pagila-12 streams data C→S: server first emits CopyInResponse, client follows
        // with CopyData frames and a terminating CopyDone.
        var service = NewService();
        var packets = service.ConvertPcap(
            "../../../../docs/examples/captures/pagila-12-copy-in-binary.pcapng",
            pgsqlPortNumber: 5434).ToList();

        var messages = packets.SelectMany(p => p.Messages).ToList();

        Assert.Contains(messages, m => m is CopyInResponseMessage);
        Assert.Contains(messages, m => m is CopyDataMessage);
        Assert.Contains(messages, m => m is CopyDoneMessage);

        var resp = messages.OfType<CopyInResponseMessage>().First();
        Assert.Equal(1, resp.OverallFormat); // binary

        var firstData = messages.OfType<CopyDataMessage>().First();
        Assert.True(firstData.IsHeader);
        Assert.NotNull(firstData.BinaryHeader);
        Assert.True(firstData.BinaryHeader!.SignatureValid);
    }

    // ── Binary-COPY header / trailer / mid-stream / text-mode CopyData ──

    [Fact]
    public void CopyDataMessage_Read_NoState_HasNullBinaryFormat()
    {
        // When no copyStream state is supplied, IsBinaryFormat should be null (unknown)
        // and no header/trailer flags should be set — preserves backwards compatibility.
        var bytes = new byte[] { 0x00, 0x00, 0x00, 0x08, 0x01, 0x02, 0x03, 0x04 };
        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('d', "CopyData", IsFrontEnd: false);

        var msg = CopyDataMessage.Read(descriptor, reader);

        Assert.Null(msg.IsBinaryFormat);
        Assert.False(msg.IsHeader);
        Assert.False(msg.IsTrailer);
        Assert.Null(msg.BinaryHeader);
    }

    [Fact]
    public void CopyDataMessage_Read_FirstBinaryChunk_ParsesFileHeader()
    {
        // Wire bytes: length(4) + signature(11) + flags(4) + extLen(4) + tuple(int16 fieldCount=2, int32 len=4, int32 val=1, int32 len=8, "PENELOPE")
        var payload = new byte[]
        {
            0x50, 0x47, 0x43, 0x4F, 0x50, 0x59, 0x0A, 0xFF, 0x0D, 0x0A, 0x00, // magic
            0x00, 0x00, 0x00, 0x00,                                           // flags = 0
            0x00, 0x00, 0x00, 0x00,                                           // ext-len = 0
            0x00, 0x02,                                                       // 2 fields
            0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01,                   // int4 = 1
            0x00, 0x00, 0x00, 0x08, 0x50, 0x45, 0x4E, 0x45, 0x4C, 0x4F, 0x50, 0x45  // "PENELOPE"
        };
        var bytes = new byte[4 + payload.Length];
        int total = 4 + payload.Length;
        bytes[0] = (byte)(total >> 24); bytes[1] = (byte)(total >> 16); bytes[2] = (byte)(total >> 8); bytes[3] = (byte)total;
        Array.Copy(payload, 0, bytes, 4, payload.Length);

        using var reader = ReaderFor(bytes);
        reader.BeginMessage();
        var descriptor = new PostgresMessageDescriptor('d', "CopyData", IsFrontEnd: false);
        var state = new CopyStreamState(CopyStreamKind.Out, CopyStreamFormat.Binary, ChunkCount: 0);

        var msg = CopyDataMessage.Read(descriptor, reader, state);
        msg.ParsedFields = reader.EndMessage();

        Assert.True(msg.IsHeader);
        Assert.True(msg.IsBinaryFormat);
        Assert.False(msg.IsTrailer);
        Assert.NotNull(msg.BinaryHeader);
        Assert.True(msg.BinaryHeader!.SignatureValid);
        Assert.Equal(0u, msg.BinaryHeader.Flags);
        Assert.Equal(0, msg.BinaryHeader.HeaderExtensionLength);

        // ParsedFields should include the named header components for hover-highlight.
        var fieldNames = msg.ParsedFields.Select(f => f.Name).ToList();
        Assert.Contains("signature", fieldNames);
        Assert.Contains("flags", fieldNames);
        Assert.Contains("headerExtensionLength", fieldNames);
        Assert.Contains("tupleData", fieldNames);

        var sigField = msg.ParsedFields.First(f => f.Name == "signature");
        Assert.Equal(11, sigField.Length);

        // PreviewBytes must still reflect the on-wire payload (first 41 bytes here, well under 64).
        Assert.Equal(payload.Length, msg.DataLength);
        Assert.Equal(payload, msg.PreviewBytes);
    }

    [Fact]
    public void CopyDataMessage_Read_MidStreamBinaryChunk_FlagsBinaryButNoHeader()
    {
        var payload = new byte[] { 0x00, 0x02, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x07 };
        var bytes = new byte[4 + payload.Length];
        int total = 4 + payload.Length;
        bytes[0] = (byte)(total >> 24); bytes[1] = (byte)(total >> 16); bytes[2] = (byte)(total >> 8); bytes[3] = (byte)total;
        Array.Copy(payload, 0, bytes, 4, payload.Length);

        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('d', "CopyData", IsFrontEnd: false);
        var state = new CopyStreamState(CopyStreamKind.Out, CopyStreamFormat.Binary, ChunkCount: 3);

        var msg = CopyDataMessage.Read(descriptor, reader, state);

        Assert.False(msg.IsHeader);
        Assert.Null(msg.BinaryHeader);
        Assert.True(msg.IsBinaryFormat);
        Assert.False(msg.IsTrailer);
    }

    [Fact]
    public void CopyDataMessage_Read_BinaryTrailer_IsDetected()
    {
        // length(4) + 0xFF 0xFF
        var bytes = new byte[] { 0x00, 0x00, 0x00, 0x06, 0xFF, 0xFF };
        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('d', "CopyData", IsFrontEnd: false);
        var state = new CopyStreamState(CopyStreamKind.Out, CopyStreamFormat.Binary, ChunkCount: 5);

        var msg = CopyDataMessage.Read(descriptor, reader, state);

        Assert.True(msg.IsTrailer);
        Assert.False(msg.IsHeader);
        Assert.True(msg.IsBinaryFormat);
        Assert.Equal(2, msg.DataLength);
    }

    [Fact]
    public void CopyDataMessage_Read_TextCopyStream_FlagsTextNoHeader()
    {
        // Text CopyData payload — a single newline-terminated record.
        var payload = Encoding.UTF8.GetBytes("alpha\tbeta\n");
        var bytes = new byte[4 + payload.Length];
        int total = 4 + payload.Length;
        bytes[0] = (byte)(total >> 24); bytes[1] = (byte)(total >> 16); bytes[2] = (byte)(total >> 8); bytes[3] = (byte)total;
        Array.Copy(payload, 0, bytes, 4, payload.Length);

        using var reader = ReaderFor(bytes);
        var descriptor = new PostgresMessageDescriptor('d', "CopyData", IsFrontEnd: true);
        var state = new CopyStreamState(CopyStreamKind.In, CopyStreamFormat.Text, ChunkCount: 0);

        var msg = CopyDataMessage.Read(descriptor, reader, state);

        Assert.False(msg.IsHeader);
        Assert.False(msg.IsTrailer);
        Assert.False(msg.IsBinaryFormat); // not null — we know it's text
        Assert.Null(msg.BinaryHeader);
    }

    [Fact]
    public void ProtocolStateProjector_TracksCopyModeAcrossBinaryStream()
    {
        var service = NewService();
        var packets = service.ConvertPcap(
            "../../../../docs/examples/captures/pagila-11-copy-out-binary.pcapng",
            pgsqlPortNumber: 5434).ToList();

        var projection = ProtocolStateProjector.Project(packets).ToList();

        var afterCopyOutResponse = projection.First(t => t.Message is CopyOutResponseMessage);
        Assert.Equal(CopyStreamKind.Out, afterCopyOutResponse.Snapshot.CopyMode);
        Assert.Equal(CopyStreamFormat.Binary, afterCopyOutResponse.Snapshot.CopyFormat);

        var afterCopyDone = projection.First(t => t.Message is CopyDoneMessage);
        Assert.Equal(CopyStreamKind.None, afterCopyDone.Snapshot.CopyMode);
        Assert.Null(afterCopyDone.Snapshot.CopyFormat);
    }
}

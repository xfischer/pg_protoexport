using System.Text;

namespace pg_protoexport.tests;

public class PcapBinaryReaderTests
{
    private static PcapBinaryReader CreateReader(byte[] data)
    {
        var stream = new MemoryStream(data);
        var binaryReader = new BinaryReader(stream, Encoding.UTF8);
        return new PcapBinaryReader(binaryReader, Encoding.UTF8);
    }

    [Fact]
    public void HasSufficientData_ExactBoundary_ReturnsTrue()
    {
        using var reader = CreateReader(new byte[10]);
        Assert.True(reader.HasSufficientData(10));
    }

    [Fact]
    public void HasSufficientData_OneBeyond_ReturnsFalse()
    {
        using var reader = CreateReader(new byte[10]);
        Assert.False(reader.HasSufficientData(11));
    }

    [Fact]
    public void HasSufficientData_AfterPartialRead_ReturnsCorrectly()
    {
        using var reader = CreateReader(new byte[10]);
        reader.ReadByte(); // consume 1 byte
        Assert.True(reader.HasSufficientData(9));
        Assert.False(reader.HasSufficientData(10));
    }

    [Fact]
    public void ReadNullTerminatedString_NormalString_ReturnsString()
    {
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00]; // "Hello\0"
        using var reader = CreateReader(data);
        var result = reader.ReadNullTerminatedString(100);
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ReadNullTerminatedString_EmptyString_ReturnsEmpty()
    {
        byte[] data = [0x00]; // just null terminator
        using var reader = CreateReader(data);
        var result = reader.ReadNullTerminatedString(100);
        Assert.Equal("", result);
    }

    [Fact]
    public void ThrowIfEndOfStream_InsufficientData_Throws()
    {
        using var reader = CreateReader(new byte[5]);
        Assert.Throws<EndOfStreamException>(() => reader.ThrowIfEndOfStream(6));
    }

    [Fact]
    public void ThrowIfEndOfStream_SufficientData_DoesNotThrow()
    {
        using var reader = CreateReader(new byte[5]);
        reader.ThrowIfEndOfStream(5); // should not throw
    }
}

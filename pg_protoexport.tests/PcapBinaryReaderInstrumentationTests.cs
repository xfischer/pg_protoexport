using System.Text;

namespace pg_protoexport.tests;

public class PcapBinaryReaderInstrumentationTests
{
    private static PcapBinaryReader CreateReader(byte[] data)
    {
        var stream = new MemoryStream(data);
        var binaryReader = new BinaryReader(stream, Encoding.UTF8);
        return new PcapBinaryReader(binaryReader, Encoding.UTF8);
    }

    [Fact]
    public void BeginField_WhenRecorderOff_ReturnsZeroCostDisposable_AndYieldsEmptyFieldList()
    {
        // arrange — recorder NOT started
        using var reader = CreateReader([0x00, 0x00, 0x00, 0x05]);

        // act — should not throw, should be a true no-op
        using (reader.BeginField("length")) reader.ReadInt32();

        var fields = reader.EndMessage();

        // assert
        Assert.Empty(fields);
    }

    [Fact]
    public void BeginField_WhenRecorderOn_CapturesOffsetAndLengthRelativeToMessageStart()
    {
        // arrange — 4 bytes of int32, then 5 bytes of a 4-char + null string
        byte[] data = [0x00, 0x00, 0x00, 0x09, (byte)'A', (byte)'B', (byte)'C', (byte)'D', 0x00];
        using var reader = CreateReader(data);

        // act
        reader.BeginMessage();
        using (reader.BeginField("length")) reader.ReadInt32();
        using (reader.BeginField("query")) reader.ReadNullTerminatedString(100);
        var fields = reader.EndMessage();

        // assert
        Assert.Collection(fields,
            f => { Assert.Equal("length", f.Name); Assert.Equal(0, f.Offset); Assert.Equal(4, f.Length); },
            f => { Assert.Equal("query", f.Name); Assert.Equal(4, f.Offset); Assert.Equal(5, f.Length); });
    }

    [Fact]
    public void BeginField_EndMessage_ClearsBufferForNextMessage()
    {
        // arrange
        using var reader = CreateReader([0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02]);

        // act — read two consecutive "messages" with the recorder
        reader.BeginMessage();
        using (reader.BeginField("first")) reader.ReadInt32();
        var firstFields = reader.EndMessage();

        reader.BeginMessage();
        using (reader.BeginField("second")) reader.ReadInt32();
        var secondFields = reader.EndMessage();

        // assert — second message's field list does not contain the first
        Assert.Single(firstFields);
        Assert.Equal("first", firstFields[0].Name);
        Assert.Single(secondFields);
        Assert.Equal("second", secondFields[0].Name);
        Assert.Equal(0, secondFields[0].Offset); // offset is reset relative to second message start
    }
}

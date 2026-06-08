namespace pg_protoexport;
public class UnknownMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public required byte[] Data { get; set; }

    internal static UnknownMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        byte[] data;
        using (reader.BeginField("data")) data = reader.ReadBytes(len - 4); // read until end
        return new UnknownMessage(pgMessage, len) { Data = data };
    }
}
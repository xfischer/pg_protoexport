namespace pg_protoexport;

public class SASLResponseMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public byte[] AuthData { get; init; } = [];

    internal static SASLResponseMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        byte[] authData;
        using (reader.BeginField("authData")) authData = reader.ReadBytes(len - 4);
        return new SASLResponseMessage(pgMessage, len)
        {
            AuthData = authData
        };
    }
}

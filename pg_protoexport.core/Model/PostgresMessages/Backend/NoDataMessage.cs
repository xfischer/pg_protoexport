namespace pg_protoexport;

public class NoDataMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    internal static NoDataMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        return new NoDataMessage(pgMessage, len);
    }
}
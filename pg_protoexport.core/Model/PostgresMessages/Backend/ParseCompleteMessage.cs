namespace pg_protoexport;

public class ParseCompleteMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    internal static ParseCompleteMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        return new ParseCompleteMessage(pgMessage, len);
    }
}
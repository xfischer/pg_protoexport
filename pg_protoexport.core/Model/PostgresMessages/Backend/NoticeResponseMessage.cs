namespace pg_protoexport;

public class NoticeResponseMessage(PostgresMessageDescriptor pgMessage, int length) : FieldListResponseMessage(pgMessage, length)
{
    internal static NoticeResponseMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        return new NoticeResponseMessage(pgMessage, len)
        {
            Fields = ReadFields(reader, len)
        };
    }
}

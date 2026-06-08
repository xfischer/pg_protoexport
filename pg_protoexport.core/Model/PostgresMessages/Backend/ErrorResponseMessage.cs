namespace pg_protoexport;

public class ErrorResponseMessage(PostgresMessageDescriptor pgMessage, int length) : FieldListResponseMessage(pgMessage, length)
{
    internal static ErrorResponseMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        return new ErrorResponseMessage(pgMessage, len)
        {
            Fields = ReadFields(reader, len)
        };
    }
}

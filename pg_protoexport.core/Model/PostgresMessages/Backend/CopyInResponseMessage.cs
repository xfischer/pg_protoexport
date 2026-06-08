namespace pg_protoexport;

public sealed class CopyInResponseMessage(PostgresMessageDescriptor pgMessage, int length) : CopyResponseBase(pgMessage, length)
{
    internal static CopyInResponseMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        var (len, fmt, cols) = ReadFields(reader);
        return new CopyInResponseMessage(pgMessage, len)
        {
            OverallFormat = fmt,
            ColumnFormats = cols
        };
    }
}

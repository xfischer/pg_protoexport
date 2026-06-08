namespace pg_protoexport;

public sealed class CopyOutResponseMessage(PostgresMessageDescriptor pgMessage, int length) : CopyResponseBase(pgMessage, length)
{
    internal static CopyOutResponseMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        var (len, fmt, cols) = ReadFields(reader);
        return new CopyOutResponseMessage(pgMessage, len)
        {
            OverallFormat = fmt,
            ColumnFormats = cols
        };
    }
}

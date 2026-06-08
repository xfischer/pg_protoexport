namespace pg_protoexport;

public sealed class CopyBothResponseMessage(PostgresMessageDescriptor pgMessage, int length) : CopyResponseBase(pgMessage, length)
{
    internal static CopyBothResponseMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        var (len, fmt, cols) = ReadFields(reader);
        return new CopyBothResponseMessage(pgMessage, len)
        {
            OverallFormat = fmt,
            ColumnFormats = cols
        };
    }
}

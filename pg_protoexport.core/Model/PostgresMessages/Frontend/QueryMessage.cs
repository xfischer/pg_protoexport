namespace pg_protoexport;

public class QueryMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public string Query { get; init; } = "";

    internal static QueryMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        string query;
        using (reader.BeginField("query")) query = reader.ReadNullTerminatedString(len);
        return new QueryMessage(pgMessage, len) { Query = query };
    }
    public override string GetStringRepresentation() => Query;
}

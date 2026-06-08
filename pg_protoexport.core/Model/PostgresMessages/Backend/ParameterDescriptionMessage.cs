namespace pg_protoexport;

public class ParameterDescriptionMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public short ParameterCount { get; internal set; }

    public List<int> ParameterOids { get; internal set; } = [];

    internal static ParameterDescriptionMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        short parameterCount;
        using (reader.BeginField("parameterCount")) parameterCount = reader.ReadInt16();
        var message = new ParameterDescriptionMessage(pgMessage, len)
        {
            ParameterCount = parameterCount
        };

        for (int i = 0; i < message.ParameterCount; i++)
        {
            int oid;
            using (reader.BeginField($"parameterOid[{i}]")) oid = reader.ReadInt32();
            message.ParameterOids.Add(oid);
        }

        return message;
    }

    public override string GetStringRepresentation() => $"oids: [{string.Join(", ", ParameterOids)}]";
}

namespace pg_protoexport;

public class ParseMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{    
    public string Statement { get; internal set; } = "";
    public string Query { get; internal set; } = "";
    public short ParameterCount { get; internal set; }
    public List<int> ParameterOids { get; internal set; } = [];

    internal static ParseMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        var packet = new ParseMessage(pgMessage, len);
        using (reader.BeginField("statementName")) packet.Statement = reader.ReadNullTerminatedString(len);
        using (reader.BeginField("query")) packet.Query = reader.ReadNullTerminatedString(len);
        using (reader.BeginField("parameterCount")) packet.ParameterCount = reader.ReadInt16();

        for (int i = 0; i < packet.ParameterCount; i++)
        {
            using (reader.BeginField($"parameterOid[{i}]"))
                packet.ParameterOids.Add(reader.ReadInt32());
        }
        return packet;
    }

    public override string GetStringRepresentation() => $"statement={Statement}, " +
        $"query={Query}, " +
        $"parameter oids=[{string.Join(", ", ParameterOids)}]";
}

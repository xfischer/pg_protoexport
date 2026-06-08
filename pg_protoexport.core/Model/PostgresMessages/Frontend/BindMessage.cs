namespace pg_protoexport;

public class BindMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public string PortalName { get; internal set; } = "";
    public string StatementName { get; internal set; } = "";
    public short ParameterCount { get; internal set; }


    public short ParameterFormatsCount { get; internal set; }
    public List<short> ParameterFormats { get; internal set; } = [];


    public short ParameterValuesCount { get; internal set; }
    public List<(int Length, byte[] Data)> ParameterValues { get; internal set; } = [];

    public short ResultsFormatCount { get; internal set; }
    public List<short> ResultsFormat { get; internal set; } = [];

    internal static BindMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        var message = new BindMessage(pgMessage, len);
        using (reader.BeginField("portalName")) message.PortalName = reader.ReadNullTerminatedString(len);
        using (reader.BeginField("statementName")) message.StatementName = reader.ReadNullTerminatedString(len);
        using (reader.BeginField("parameterFormatsCount")) message.ParameterFormatsCount = reader.ReadInt16();

        for (int i = 0; i < message.ParameterFormatsCount; i++)
        {
            using (reader.BeginField($"parameterFormat[{i}]"))
                message.ParameterFormats.Add(reader.ReadInt16());
        }

        using (reader.BeginField("parameterValuesCount")) message.ParameterValuesCount = reader.ReadInt16();
        for (int i = 0; i < message.ParameterValuesCount; i++)
        {
            int paramLength;
            using (reader.BeginField($"parameterLength[{i}]")) paramLength = reader.ReadInt32();
            if (paramLength > 0)
            {
                byte[] data;
                using (reader.BeginField($"parameterValue[{i}]")) data = reader.ReadBytes(paramLength);
                message.ParameterValues.Add(new(paramLength, data));
            }
            else
            {
                message.ParameterValues.Add(new(paramLength, []));
            }
        }

        using (reader.BeginField("resultsFormatCount")) message.ResultsFormatCount = reader.ReadInt16();
        for (int i = 0; i < message.ResultsFormatCount; i++)
        {
            using (reader.BeginField($"resultsFormat[{i}]"))
                message.ResultsFormat.Add(reader.ReadInt16());
        }
        return message;
    }

    public override string GetStringRepresentation() => $"{PortalName}{StatementName}: " +
        $"formats: [{string.Join(", ", ParameterFormats)}], " +
        $"values len: [{string.Join(", ", ParameterValues.Select(v => v.Length))}], " +
        $"results format: [{string.Join(", ", ResultsFormat)}]";
}

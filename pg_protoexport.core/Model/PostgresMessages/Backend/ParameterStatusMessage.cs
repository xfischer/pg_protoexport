namespace pg_protoexport;

public class ParameterStatusMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public string ParameterName { get; init; } = "";
    public string Value { get; init; } = "";

    internal static ParameterStatusMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        string name, value;
        using (reader.BeginField("parameterName")) name = reader.ReadNullTerminatedString(len);
        using (reader.BeginField("value")) value = reader.ReadNullTerminatedString(len);
        return new ParameterStatusMessage(pgMessage, len)
        {
            ParameterName = name,
            Value = value
        };
    }

    public override string GetStringRepresentation() => $"{ParameterName}: {Value}";
}
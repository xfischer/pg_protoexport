namespace pg_protoexport;

/// <summary>
/// Base class for messages that contain a list of typed field entries (ErrorResponse, NoticeResponse).
/// </summary>
public abstract class FieldListResponseMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public List<(char FieldType, string Message)> Fields { get; init; } = [];

    protected static List<(char FieldType, string Message)> ReadFields(PcapBinaryReader reader, int len)
    {
        var fields = new List<(char, string)>();
        int index = 0;
        char fieldType;
        using (reader.BeginField($"fieldType[{index}]")) fieldType = reader.ReadChar();
        do
        {
            if (fieldType == 0)
            {
                using (reader.BeginField($"fieldType[{index}]")) fieldType = reader.ReadChar();
            }
            else
            {
                string fieldMessage;
                using (reader.BeginField($"fieldMessage[{index}]")) fieldMessage = reader.ReadNullTerminatedString(len);
                fields.Add((fieldType, fieldMessage));
                index++;
                using (reader.BeginField($"fieldType[{index}]")) fieldType = reader.ReadChar();
            }
        }
        while (fieldType != 0);

        return fields;
    }

    public override string GetStringRepresentation()
        => $"[{string.Join(", ", Fields.Select(f => $"{f.FieldType}: {f.Message}"))}]";
}

namespace pg_protoexport;

public class RowDescriptionMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{

    public short FieldCount { get; internal set; }

    public List<FieldDescription> FieldDescriptions { get; internal set; } = [];

    internal static RowDescriptionMessage? Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        if (!reader.HasSufficientData(4))
            return null;
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();

        if (!reader.HasSufficientData(len))
            return null;

        var message = new RowDescriptionMessage(pgMessage, len);
        using (reader.BeginField("fieldCount")) message.FieldCount = reader.ReadInt16();

        for (int i = 0; i < message.FieldCount; i++)
        {
            message.FieldDescriptions.Add(FieldDescription.Read(reader, len, i));
        }

        return message;
    }

    public override string GetStringRepresentation() => $"[{string.Join(", ", 
        FieldDescriptions.Select(f => $"{f.ColumnName}: {f.TableOid}, {f.ColumnIndex}, {f.TypeOid}, {f.ColumnLength}, {f.TypeModifier}, {f.Format}"))}]";
}

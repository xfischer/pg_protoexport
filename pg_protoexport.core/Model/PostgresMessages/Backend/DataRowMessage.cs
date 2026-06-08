using System.Text;

namespace pg_protoexport;

public class DataRowMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public record Row(int Length, bool IsText, byte[]? Data, string? TextRepresentation)
    {
        public string? Name { get; init; }
    }

    public short FieldCount { get; internal set; }

    public List<Row> ColumnValues { get; internal set; } = [];

    internal static DataRowMessage? Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader, RowDescriptionMessage? lastRowDescription)
    {
        if (!reader.HasSufficientData(4))
            return null;

        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        if (!reader.HasSufficientData(len))
            return null;

        var message = new DataRowMessage(pgMessage, len);
        using (reader.BeginField("fieldCount")) message.FieldCount = reader.ReadInt16();

        for (int i = 0; i < message.FieldCount; i++)
        {
            int colLength;
            using (reader.BeginField($"columnLength[{i}]")) colLength = reader.ReadInt32();
            // TODO: create converters, should be also useful for Bind and other messages with data
            bool isText = lastRowDescription?.FieldDescriptions[i].Format == 0
                            || lastRowDescription?.FieldDescriptions[i].TypeOid == 19
                            || lastRowDescription?.FieldDescriptions[i].TypeOid == 18;
            byte[]? data = null;
            string? text = null;
            if (colLength > 0)
            {
                using var scope = reader.BeginField($"columnValue[{i}]");
                data = reader.ReadBytes(colLength);
                text = isText ? Encoding.UTF8.GetString(data) : Convert.ToHexStringLower(data);
                scope.SetValue(text);
            }
            var row = new Row(colLength, isText, data, text) { Name = lastRowDescription?.FieldDescriptions[i].ColumnName };
            message.ColumnValues.Add(row);
        }

        return message;
    }

    public override string GetStringRepresentation() 
        => $"[{string.Join(", ", ColumnValues.Select(c => $"{c.Name}:{c.TextRepresentation}"))}]";
}

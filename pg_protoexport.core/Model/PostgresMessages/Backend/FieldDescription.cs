namespace pg_protoexport;

public record FieldDescription(string ColumnName, int TableOid, short ColumnIndex,
    int TypeOid, short ColumnLength, int TypeModifier, short Format)
{
    internal static FieldDescription Read(PcapBinaryReader reader, int maxLength, int index = 0)
    {
        string columnName;
        int tableOid, typeOid, typeModifier;
        short columnIndex, columnLength, format;
        using (reader.BeginField($"columnName[{index}]")) columnName = reader.ReadNullTerminatedString(maxLength);
        using (reader.BeginField($"tableOid[{index}]")) tableOid = reader.ReadInt32();
        using (reader.BeginField($"columnIndex[{index}]")) columnIndex = reader.ReadInt16();
        using (reader.BeginField($"typeOid[{index}]")) typeOid = reader.ReadInt32();
        using (reader.BeginField($"columnLength[{index}]")) columnLength = reader.ReadInt16();
        using (reader.BeginField($"typeModifier[{index}]")) typeModifier = reader.ReadInt32();
        using (reader.BeginField($"format[{index}]")) format = reader.ReadInt16();
        return new FieldDescription(columnName, tableOid, columnIndex, typeOid, columnLength, typeModifier, format);
    }
}

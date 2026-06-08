namespace pg_protoexport;

/// <summary>
/// Shared shape for the three Copy-response messages (CopyInResponse 'G', CopyOutResponse 'H',
/// CopyBothResponse 'W'). They have identical wire format: a 1-byte overall format
/// (0 = text, 1 = binary) followed by an Int16 column count and an Int16 array of per-column
/// format codes.
/// </summary>
public abstract class CopyResponseBase(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    /// <summary>0 = text format, 1 = binary format.</summary>
    public byte OverallFormat { get; init; }

    /// <summary>Per-column format codes (0 = text, 1 = binary). One entry per result column.</summary>
    public List<short> ColumnFormats { get; init; } = [];

    protected static (int Length, byte OverallFormat, List<short> Cols) ReadFields(PcapBinaryReader reader)
    {
        int len;
        byte fmt;
        short n;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        using (reader.BeginField("overallFormat")) fmt = reader.ReadByte();
        using (reader.BeginField("columnCount")) n = reader.ReadInt16();
        var cols = new List<short>(n);
        for (int i = 0; i < n; i++)
        {
            short col;
            using (reader.BeginField($"columnFormat[{i}]")) col = reader.ReadInt16();
            cols.Add(col);
        }
        return (len, fmt, cols);
    }

    public override string GetStringRepresentation() =>
        $"format={(OverallFormat == 1 ? "binary" : "text")}, columns=[{string.Join(", ", ColumnFormats)}]";
}

using static pg_protoexport.LatexHelper;

namespace pg_protoexport.Templates;

public partial class DataRow : ITextTransformer
{
    public DataRowMessage Message { get; }
    public int Length { get; }
    public int FieldCount { get; }

    public List<(int Length, string Name, string? Data, string RawText, bool IsText)> Fields { get; } = [];

    public DataRow(DataRowMessage message)
    {
        Message = message;
        Length = message.Length;
        FieldCount = message.FieldCount;
        int index = 0;
        foreach (var f in message.ColumnValues)
        {
            var fieldLabel = string.IsNullOrEmpty(f.Name) ? $"field {index}" : $"field {index}  \\\\ {TrimUnescape(f.Name, 25)}";
            string rawText;
            string? display;
            if (f.Length > 0)
            {
                if (f.IsText && f.TextRepresentation != null)
                {
                    rawText = f.TextRepresentation;
                    display = TrimUnescape(f.TextRepresentation, 50);
                }
                else
                {
                    rawText = BitConverter.ToString(f.Data!).Replace("-", ":").ToLower();
                    display = TrimUnescape(rawText, 50);
                }
            }
            else
            {
                rawText = string.Empty;
                display = string.Empty;
            }
            Fields.Add((f.Length, fieldLabel, display, rawText, f.IsText));
            index++;
        }
    }


}

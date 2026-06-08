namespace pg_protoexport.Templates;

public partial class CopyOutResponse(CopyOutResponseMessage message) : ITextTransformer
{
    public CopyOutResponseMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public byte OverallFormat { get; } = message.OverallFormat;
    public int ColumnCount { get; } = message.ColumnFormats.Count;
    public List<short> ColumnFormats { get; } = message.ColumnFormats;
}

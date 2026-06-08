namespace pg_protoexport.Templates;

public partial class CopyInResponse(CopyInResponseMessage message) : ITextTransformer
{
    public CopyInResponseMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public byte OverallFormat { get; } = message.OverallFormat;
    public int ColumnCount { get; } = message.ColumnFormats.Count;
    public List<short> ColumnFormats { get; } = message.ColumnFormats;
}

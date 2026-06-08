namespace pg_protoexport.Templates;

public partial class ErrorResponse(ErrorResponseMessage message) : ITextTransformer
{
    public ErrorResponseMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public List<(char FieldType, string Message, string RawMessage)> Messages { get; } = message.Fields
        .Select(f => (f.FieldType, LatexHelper.TrimUnescape(f.Message, 75), f.Message ?? string.Empty))
        .ToList();
}

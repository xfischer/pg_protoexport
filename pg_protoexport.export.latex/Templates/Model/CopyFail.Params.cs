using static pg_protoexport.LatexHelper;

namespace pg_protoexport.Templates;

public partial class CopyFail(CopyFailMessage message) : ITextTransformer
{
    public CopyFailMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public string ErrorMessageText { get; } = Unescape(message.ErrorMessage);
}

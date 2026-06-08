using static pg_protoexport.LatexHelper;

namespace pg_protoexport.Templates;

public partial class CommandComplete(CommandCompleteMessage message) : ITextTransformer
{
    public CommandCompleteMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public string Tag { get; } = Unescape(message.Message);
}

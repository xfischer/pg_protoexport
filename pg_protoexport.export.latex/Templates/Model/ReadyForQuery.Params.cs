using static pg_protoexport.LatexHelper;

namespace pg_protoexport.Templates;

public partial class ReadyForQuery(ReadyForQueryMessage message) : ITextTransformer
{
    public ReadyForQueryMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public string Status { get; } = Unescape(message.Status.ToString());
}

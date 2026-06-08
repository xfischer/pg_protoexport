using static pg_protoexport.LatexHelper;

namespace pg_protoexport.Templates;

public partial class Execute(ExecuteMessage message) : ITextTransformer
{
    public ExecuteMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public string Portal { get; } = Unescape(message.PortalName);
    public int MaxRows { get; } = message.MaxRows;
}

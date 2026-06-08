using static pg_protoexport.LatexHelper;

namespace pg_protoexport.Templates;

public partial class Parse(ParseMessage message) : ITextTransformer
{
    public ParseMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public string Statement { get; } = Unescape(message.Statement);
    public int StatementLength { get; } = message.Statement.Length;
    public string Query { get; } = Unescape(message.Query);
    public int QueryLength { get; } = message.Query.Length;
    public List<int> ParameterOids { get; } = message.ParameterOids;
}

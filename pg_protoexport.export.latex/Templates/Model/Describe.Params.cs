using static pg_protoexport.LatexHelper;

namespace pg_protoexport.Templates;

public partial class Describe : ITextTransformer
{
    public DescribeMessage Message { get; }
    public int Length { get; }
    public string StatementOrPortalCode { get; }
    public string StatementOrPortalName { get; }
    public string StatementOrPortalCaption { get; }

    public Describe(DescribeMessage message) {
        Message = message;
        Length = message.Length;
        StatementOrPortalCode = message.PortalOrStatement.ToString();
        StatementOrPortalName = Unescape(message.PortalOrStatementName);
        StatementOrPortalCaption = StatementOrPortalCode == "S" ? "statement name: " : "portal name: ";
        StatementOrPortalCaption += StatementOrPortalName;
    }
}

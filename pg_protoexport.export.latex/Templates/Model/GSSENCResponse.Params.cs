namespace pg_protoexport.Templates;

public partial class GSSENCResponse(GSSENCResponseMessage message) : ITextTransformer
{
    public string Type { get; } = nameof(GSSENCResponse);
    public bool Accepted { get; } = message.Accepted;
    public string Response { get; } = message.Accepted ? "G" : "N";
}

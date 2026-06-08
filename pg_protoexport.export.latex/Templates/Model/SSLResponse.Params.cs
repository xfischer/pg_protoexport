namespace pg_protoexport.Templates;

public partial class SSLResponse(SSLResponseMessage message) : ITextTransformer
{
    public string Type { get; } = nameof(SSLResponse);
    public bool Accepted { get; } = message.Accepted;
    public string Response { get; } = message.Accepted ? "S" : "N";
}

namespace pg_protoexport.Templates;

public partial class SSLRequest(SSLRequestMessage message) : ITextTransformer
{
    public string Type { get; } = nameof(SSLRequest);
    public int Length { get; } = message.Length;
    public int Payload { get; } = message.Payload;
}

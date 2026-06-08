namespace pg_protoexport.Templates;

public partial class GSSENCRequest(GSSENCRequestMessage message) : ITextTransformer
{
    public string Type { get; } = nameof(GSSENCRequest);
    public int Length { get; } = message.Length;
    public int Payload { get; } = message.Payload;
}

namespace pg_protoexport.Templates;

public partial class CancelRequest(CancelRequestMessage message) : ITextTransformer
{
    public string Type { get; } = nameof(CancelRequest);
    public int Length { get; } = message.Length;
    public int RequestCode { get; } = message.RequestCode;
    public int ProcessId { get; } = message.ProcessId;
    public int SecretKey { get; } = message.SecretKey;
}

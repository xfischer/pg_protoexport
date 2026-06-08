namespace pg_protoexport.Templates;

public partial class BackendKeyData(BackendKeyDataMessage message) : ITextTransformer
{
    public BackendKeyDataMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public int ProcessID { get; } = message.ProcessId;
    public uint SecretKey { get; } = message.SecretKey;
}

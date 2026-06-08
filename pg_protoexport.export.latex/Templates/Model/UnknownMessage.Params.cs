namespace pg_protoexport.Templates;

public partial class Unknown(UnknownMessage message) : ITextTransformer
{
    public UnknownMessage Message { get; } = message;
    public char Code { get; } = message.Code;
    public int Length { get; } = message.Length;

    /// <summary>Hex of the raw data bytes — derived conversion, kept on Params.</summary>
    public string DataHex { get; } = Convert.ToHexStringLower(message.Data);
}

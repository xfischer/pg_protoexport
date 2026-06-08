using static pg_protoexport.LatexHelper;

namespace pg_protoexport.Templates;

public partial class SASLResponse(SASLResponseMessage message) : ITextTransformer
{
    public SASLResponseMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public string Data { get; set; } = TrimUnescape("AuthData: " + Convert.ToHexStringLower(message.AuthData), 50);

    /// <summary>Hex of the raw auth data bytes — derived conversion, kept on Params.</summary>
    public string DataHex { get; } = Convert.ToHexStringLower(message.AuthData);
}

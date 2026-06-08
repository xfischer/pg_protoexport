using static pg_protoexport.LatexHelper;

namespace pg_protoexport.Templates;

public partial class SASLInitialResponse(SASLInitialResponseMessage message) : ITextTransformer
{
    public SASLInitialResponseMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public string Mechanism { get; set; } = Unescape(message.Mechanism);
    public int InitialResponseLength { get; set; } = message.InitialResponseLength;
    public string InitialResponse { get; set; } = TrimUnescape("Initial response: " + Convert.ToHexStringLower(message.InitialResponse), 50);

    /// <summary>Hex of <see cref="SASLInitialResponseMessage.InitialResponse"/> — kept here because it's a derived conversion, not a forward.</summary>
    public string InitialResponseHex { get; } = Convert.ToHexStringLower(message.InitialResponse);
}

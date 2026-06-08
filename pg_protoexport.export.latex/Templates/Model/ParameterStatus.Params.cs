namespace pg_protoexport.Templates;

public partial class ParameterStatus(ParameterStatusMessage message) : ITextTransformer
{
    public ParameterStatusMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public string ParamName { get; } = LatexHelper.TrimUnescape(message.ParameterName, 50);
    public string ParamValue { get; } = LatexHelper.TrimUnescape(message.Value, 50);
}

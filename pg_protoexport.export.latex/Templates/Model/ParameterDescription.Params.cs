namespace pg_protoexport.Templates;

public partial class ParameterDescription(ParameterDescriptionMessage message) : ITextTransformer
{
    public ParameterDescriptionMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public int ParamCount { get; } = message.ParameterCount;
    public List<int> ParameterTypes { get; } = message.ParameterOids;
}

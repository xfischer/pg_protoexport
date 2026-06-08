namespace pg_protoexport.Templates;

public partial class Bind(BindMessage message) : ITextTransformer
{
    public BindMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public string Statement { get; } = message.StatementName;
    public int ParamFormatCount { get; } = message.ParameterFormatsCount;
    public List<short> ParamFormats { get; } = message.ParameterFormats;
    public short ParamValuesCount { get; } = message.ParameterValuesCount;
    public List<(int Length, byte[] Data)> ParamValues { get; } = message.ParameterValues;
    public short ResultFormatsCount { get; } = message.ResultsFormatCount;
    public List<short> ResultFormats { get; } = message.ResultsFormat;
    public string Portal { get; } = message.PortalName;
}

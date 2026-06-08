namespace pg_protoexport.Templates;

public partial class StartupMessage : ITextTransformer
{
    public StartupMessageMessage Message { get; }
    public string Type { get; }
    public int Length { get; }
    public short VersionMajor { get; }
    public short VersionMinor { get; }
    public List<(string ParamName, string ParamValue, string RawName, string RawValue)> Parameters { get; }

    public StartupMessage(StartupMessageMessage message)
    {
        Message = message;
        Type = LatexHelper.Unescape(nameof(StartupMessage));
        Length = message.Length;
        VersionMajor = message.ProtocolMajorVersion;
        VersionMinor = message.ProtocolMinorVersion;

        Parameters = [];
        foreach (var kvp in message.Parameters)
        {
            Parameters.Add((
                LatexHelper.TrimUnescape(kvp.Key, 50),
                LatexHelper.TrimUnescape(kvp.Value, 50),
                kvp.Key ?? string.Empty,
                kvp.Value ?? string.Empty));
        }
    }
}

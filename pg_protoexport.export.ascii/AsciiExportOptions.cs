namespace pg_protoexport;

/// <summary>
/// Per-export options carrier for the ASCII exporter. Null values fall back to the DI-bound
/// defaults on <see cref="PcapToAsciiOptions"/>.
/// </summary>
public sealed record AsciiExportOptions(int? MaxLineWidth = null, int? MaxDataRows = null) : IExportOptions
{
    public static readonly AsciiExportOptions Default = new();
}

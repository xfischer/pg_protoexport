namespace pg_protoexport;

/// <summary>
/// Per-export options carrier for the ASCII exporter. Null values fall back to the DI-bound
/// defaults on <see cref="PcapToAsciiOptions"/>.
/// </summary>
public sealed record AsciiExportOptions(int? MaxLineWidth = null, int? MaxDataRows = null, bool ToConsole = false, bool ShowTimeline = false) : IExportOptions, ITimelineExportOptions
{
    public static readonly AsciiExportOptions Default = new();

    public IExportOptions WithTimeline(bool showTimeline) => this with { ShowTimeline = showTimeline };
}

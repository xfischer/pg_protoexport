namespace pg_protoexport;

/// <summary>
/// Options for the HTML exporter passed via <see cref="IPcapExporter.Export"/>.
/// </summary>
public sealed record HtmlExportOptions(bool ShowTimeline = false) : IExportOptions, ITimelineExportOptions
{
    public static readonly HtmlExportOptions Default = new();

    public IExportOptions WithTimeline(bool showTimeline) => this with { ShowTimeline = showTimeline };
}

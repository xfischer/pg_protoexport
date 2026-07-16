namespace pg_protoexport;

/// <summary>
/// Marker interface for exporter-specific option records (e.g. <c>LatexExportOptions</c>).
/// Exporters with no configurable options pass <c>null</c> when invoking
/// <see cref="IPcapExporter.Export"/>.
/// </summary>
public interface IExportOptions
{
}

/// <summary>
/// Implemented by exporter options records that support the "time since last packet" timeline
/// annotation, so <c>batchexport</c> can apply its own <c>--timeline</c> flag uniformly across
/// every exporter without knowing each options record's concrete shape.
/// </summary>
public interface ITimelineExportOptions : IExportOptions
{
    /// <summary>Returns a copy of these options with <see cref="PacketTimeline"/> annotations
    /// turned on or off.</summary>
    IExportOptions WithTimeline(bool showTimeline);
}

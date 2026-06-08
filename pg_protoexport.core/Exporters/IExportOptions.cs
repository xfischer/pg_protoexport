namespace pg_protoexport;

/// <summary>
/// Marker interface for exporter-specific option records (e.g. <c>LatexExportOptions</c>).
/// Exporters with no configurable options pass <c>null</c> when invoking
/// <see cref="IPcapExporter.Export"/>.
/// </summary>
public interface IExportOptions
{
}

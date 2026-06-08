namespace pg_protoexport;

/// <summary>
/// Unified contract for all PCAP exporters. Each exporter project implements this
/// alongside its typed <c>IPcapTo{Format}Service</c> interface; the typed interface
/// stays for direct callers (BasicSample, tests), and <c>IPcapExporter</c> is what the
/// CLI host dispatches through. New exporters should implement this contract so the
/// host can drive them generically.
/// </summary>
public interface IPcapExporter
{
    /// <summary>Lower-case identifier matching the CLI command name, e.g. "latex", "mermaid", "plantuml", "pqtrace", "html".</summary>
    string Name { get; }

    /// <summary>Default file extension (with leading dot) used by the CLI when an output path is omitted.</summary>
    string DefaultExtension { get; }

    /// <summary>
    /// Runs the export. <paramref name="mode"/> selects the output shape for multi-mode exporters
    /// (Mermaid, PlantUML) and is ignored by single-shape exporters; an invalid value throws.
    /// <paramref name="options"/> is exporter-specific; pass <c>null</c> for defaults.
    /// </summary>
    IExportResult Export(
        IEnumerable<PostgresPacket> packets,
        string outputPath,
        string? mode,
        IExportOptions? options);
}

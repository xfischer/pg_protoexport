namespace pg_protoexport;

/// <summary>
/// One output an exporter contributes to the <c>batchexport</c> command. Exporters declare their
/// variants through <see cref="IExporterCliModule.BatchVariants"/> so the batch command never needs
/// to know about specific exporters.
/// </summary>
/// <param name="Exporter">Exporter name, matching <see cref="IPcapExporter.Name"/>.</param>
/// <param name="Mode">Mode argument for multi-mode exporters; <c>null</c> for single-shape exporters.</param>
/// <param name="Options">Exporter-specific options, or <c>null</c> for defaults.</param>
/// <param name="OutputFileName">File name written under each input's batch subfolder.</param>
public sealed record BatchVariant(string Exporter, string? Mode, IExportOptions? Options, string OutputFileName);

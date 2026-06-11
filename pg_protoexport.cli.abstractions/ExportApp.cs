using Microsoft.Extensions.Logging;

namespace pg_protoexport;

public class ExportApp(
    IPcapService pcapService,
    IPcapPortDetector portDetector,
    IEnumerable<IPcapExporter> exporters,
    ILogger<ExportApp> logger) : IExportApp
{
    public IExportResult RunExport(
        string exporterName,
        string inputFile,
        string outputPath,
        ushort? port,
        string? mode = null,
        IExportOptions? options = null)
    {
        var exporter = ResolveExporter(exporterName);

        var displayName = mode is null ? exporter.Name : $"{exporter.Name} {mode}";
        logger.LogInformation("PCAP to {Name}. Processing file '{File}'...", displayName, Path.GetFileName(inputFile));
        if (!string.IsNullOrEmpty(outputPath))
            logger.LogInformation("Output path {OutputPath}", outputPath);

        ushort resolvedPort;
        if (port.HasValue)
        {
            resolvedPort = port.Value;
        }
        else
        {
            resolvedPort = portDetector.Detect(inputFile);
            logger.LogInformation("Auto-detected PostgreSQL port {Port} for {File}", resolvedPort, Path.GetFileName(inputFile));
        }

        var packets = pcapService.ConvertPcap(inputFile, resolvedPort);
        return RunExportInternal(exporter, packets, outputPath, mode, options);
    }

    // Used by callers that need to fan one parsed capture out to multiple exporters/variants
    // (see BatchExportCommand). Parsing once and replaying the materialised packet list to
    // each exporter is much cheaper than re-running ConvertPcap per variant.
    public IExportResult RunExportPrebuilt(
        IReadOnlyList<PostgresPacket> packets,
        string exporterName,
        string outputPath,
        string? mode = null,
        IExportOptions? options = null)
    {
        var exporter = ResolveExporter(exporterName);

        var displayName = mode is null ? exporter.Name : $"{exporter.Name} {mode}";
        logger.LogInformation("PCAP to {Name}. Output path {OutputPath}", displayName, outputPath);

        return RunExportInternal(exporter, packets, outputPath, mode, options);
    }

    private IPcapExporter ResolveExporter(string exporterName) =>
        exporters.FirstOrDefault(e => e.Name == exporterName)
            ?? throw new InvalidOperationException($"No exporter named '{exporterName}' is registered.");

    private IExportResult RunExportInternal(
        IPcapExporter exporter,
        IEnumerable<PostgresPacket> packets,
        string outputPath,
        string? mode,
        IExportOptions? options)
    {
        IExportResult result = new EmptyExportResult();
        try
        {
            result = exporter.Export(packets, outputPath, mode, options);
            if (!string.IsNullOrEmpty(outputPath))
                logger.LogInformation("File written to {OutputPath}", outputPath);
            return result;
        }
        catch (Exception ex) when (ex is IOException or pg_protoexportException)
        {
            // Recoverable I/O or domain faults are logged and degraded: any messages parsed before
            // the fault may already be written to the output file. Programmer errors (bad mode,
            // null refs, etc.) and cancellation deliberately propagate so they are not masked —
            // BatchExportCommand and the top-level host handler turn them into a non-zero exit.
            logger.LogError(ex, "An error has occurred: {Message}. Messages may still have been processed and written to output file.", ex.Message);
            return result;
        }
        finally
        {
            // Exporters that track stats return a non-EmptyExportResult; always log their counters
            // (including zeros) to preserve the historical reporting behavior used by callers and
            // by the end-to-end tests. Exporters that don't track stats return EmptyExportResult
            // and we stay quiet.
            if (result is not EmptyExportResult)
            {
                logger.LogInformation("{StatsPacketsProcessed} packet(s) processed. {StatsMessagesProcessed} messages written.",
                    result.PacketsProcessed, result.MessagesProcessed);
                if (result.MessagesInvalid > 0)
                    logger.LogWarning("{StatsMessagesInvalid} ignored or invalid.", result.MessagesInvalid);
            }
        }
    }
}

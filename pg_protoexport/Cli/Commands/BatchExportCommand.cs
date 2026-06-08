using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace pg_protoexport;

public class BatchExportCommand(
    IExportApp app,
    IPcapService pcapService,
    IPcapPortDetector portDetector,
    IEnumerable<IExporterCliModule> modules,
    ILogger<BatchExportCommand> logger)
    : Command<BatchExportSettings>
{
    // Every variant is contributed by an exporter's IExporterCliModule, so this command never
    // needs to know which exporters exist.
    private readonly BatchVariant[] _variants = modules.SelectMany(m => m.BatchVariants).ToArray();

    protected override int Execute(CommandContext context, BatchExportSettings settings, CancellationToken cancellation)
    {
        var inputDir  = settings.InputDir;
        var outputDir = settings.OutputDir!;
        var explicitPort = settings.Port;
        var search    = settings.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        Directory.CreateDirectory(outputDir);

        var pcaps = Directory.EnumerateFiles(inputDir, "*.pcapng", search)
            .Concat(Directory.EnumerateFiles(inputDir, "*.pcap", search))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        if (pcaps.Count == 0)
        {
            logger.LogWarning("no .pcapng / .pcap files found under {Dir}", Path.GetFullPath(inputDir));
            return 0;
        }

        logger.LogInformation(
            "found {Count} capture file(s); exporting {Variants} variant(s) each to {Out} (port {Port})",
            pcaps.Count, _variants.Length, Path.GetFullPath(outputDir),
            explicitPort.HasValue ? explicitPort.Value.ToString() : "auto-detect");

        int successes = 0;
        foreach (var path in pcaps)
        {
            var stem = Path.GetFileNameWithoutExtension(path);

            ushort port;
            if (explicitPort.HasValue)
            {
                port = explicitPort.Value;
            }
            else
            {
                try
                {
                    port = portDetector.Detect(path);
                    logger.LogInformation("{Stem}: auto-detected port {Port}", stem, port);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Stem}: could not auto-detect port; skipping all variants", stem);
                    continue;
                }
            }

            logger.LogInformation("{Stem}: parsing {Path}", stem, path);
            List<PostgresPacket> packets;
            try
            {
                packets = pcapService.ConvertPcap(path, port).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Stem}: parse failed; skipping all variants", stem);
                continue;
            }

            if (packets.Count == 0)
            {
                logger.LogWarning("{Stem}: no PostgreSQL packets at port {Port}; skipping all variants", stem, port);
                continue;
            }

            var subDir = Path.Combine(outputDir, stem);
            Directory.CreateDirectory(subDir);

            bool fileOk = true;
            foreach (var v in _variants)
            {
                var outPath = Path.Combine(subDir, v.OutputFileName);
                try
                {
                    app.RunExportPrebuilt(packets, v.Exporter, outPath, v.Mode, v.Options);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Stem}: variant {Exporter}/{Mode} failed",
                        stem, v.Exporter, v.Mode ?? "<default>");
                    fileOk = false;
                }
            }
            if (fileOk) successes++;
        }

        logger.LogInformation("done; {OK}/{Total} input files fully exported", successes, pcaps.Count);
        return successes == pcaps.Count ? 0 : 1;
    }
}

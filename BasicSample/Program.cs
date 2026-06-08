using Microsoft.Extensions.Logging;
using pg_protoexport;

namespace BasicSample;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = loggerFactory.CreateLogger<Program>();

        string verb = args.Length > 0 ? args[0].ToLowerInvariant() : "read";
        return verb switch
        {
            "read"           => await RunReadAsync(loggerFactory, logger),
            "-h" or "--help" => PrintUsage(0),
            _                => PrintUsage(2, $"unknown verb '{verb}'"),
        };
    }

    private static async Task<int> RunReadAsync(ILoggerFactory loggerFactory, ILogger logger)
    {
        string inputFile = "extendedQuery.pcapng";
        ushort portNumber = 5432;

        logger.LogInformation("Starting...");

        // Capture
        var pcap = PcapService.Create(loggerFactory);
        var pgPackets = pcap.ConvertPcap(inputFile, portNumber).ToList();

        logger.LogInformation("{Count} packet(s) retrieved", pgPackets.Count);


        // Transform to LaTeX
        var latexFile = Path.ChangeExtension(inputFile, ".tex");
        var latex = PcapToLatexService.Create(loggerFactory);
        latex.PcapToLaTeX(pgPackets, latexFile, standalone: true);
        logger.LogInformation("LaTeX file written to {File}", Path.GetFullPath(latexFile));


        // Transform to anything!
        // here PlantUML sequence diagram in Markdown
        var markdownFile = Path.ChangeExtension(inputFile, ".md");
        var markdown = MarkdownPlantUmlGenerator.GenerateMarkdownPlantUml(pgPackets);

        await File.WriteAllTextAsync(markdownFile, markdown);
        logger.LogInformation("Markdown/PlantUML file written to {File}", Path.GetFullPath(markdownFile));


        // Transform to Mermaid sequence diagram in Markdown
        var mermaidFile = Path.ChangeExtension(inputFile, ".mermaid.md");
        var mermaid = MarkdownMermaidGenerator.GenerateMarkdownMermaid(pgPackets);

        await File.WriteAllTextAsync(mermaidFile, mermaid);
        logger.LogInformation("Markdown/Mermaid file written to {File}", Path.GetFullPath(mermaidFile));

        return 0;
    }

    private static int PrintUsage(int exitCode, string? error = null)
    {
        if (error is not null)
            Console.Error.WriteLine(error);
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  dotnet run                # default: read extendedQuery.pcapng and export to LaTeX / Markdown");
        Console.Error.WriteLine("  dotnet run -- read        # same as default");
        Console.Error.WriteLine();
        Console.Error.WriteLine("For the pagila workload + per-scenario capture, see the");
        Console.Error.WriteLine("pg_protoexport.samples.pagila project:");
        Console.Error.WriteLine("  dotnet run --project pg_protoexport.samples.pagila -- --help");
        return exitCode;
    }
}

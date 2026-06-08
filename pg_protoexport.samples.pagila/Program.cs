using Microsoft.Extensions.Logging;

namespace pg_protoexport;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = loggerFactory.CreateLogger<Program>();

        string verb = args.Length > 0 ? args[0].ToLowerInvariant() : "generate";
        return verb switch
        {
            "generate"             => await PagilaTrafficGenerator.RunAsync(logger, CancellationToken.None),
            "capture-and-generate" => await RunCaptureAndGenerateAsync(loggerFactory, logger, args),
            "-h" or "--help"       => PrintUsage(0),
            _                      => PrintUsage(2, $"unknown verb '{verb}'"),
        };
    }

    private static async Task<int> RunCaptureAndGenerateAsync(
        ILoggerFactory loggerFactory, ILogger logger, string[] args)
    {
        string basePath = args.Length > 1 ? args[1] : "pagila.pcapng";

        var csb = PagilaTrafficGenerator.BuildConnectionString();
        var baseOptions = new LiveCaptureOptions(basePath)
        {
            Host = csb.Host ?? "localhost",
            Port = (ushort)csb.Port,
            EchoPackets = true,
        };

        logger.LogInformation(
            "starting per-scenario capture (host={Host} port={Port}); base name = {Output}",
            baseOptions.Host, baseOptions.Port, Path.GetFullPath(basePath));
        logger.LogInformation(
            "note: Terminate (X) is emitted on connection dispose, after the last scenario, and will not appear in any file");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        async Task ScenarioWrapper(int index, string title, Func<Task> body)
        {
            var perFile = DerivePerScenarioPath(basePath, index, title);
            var perOptions = baseOptions with { OutputFile = perFile };
            await using var session = await LiveCaptureSession.StartAsync(perOptions, loggerFactory, cts.Token);
            await body();
        }

        int result = await PagilaTrafficGenerator.RunAsync(logger, cts.Token, ScenarioWrapper);

        logger.LogInformation("done; capture files written next to {Output}", Path.GetFullPath(basePath));
        return result;
    }

    private static string DerivePerScenarioPath(string basePath, int index, string title)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(basePath)) ?? ".";
        var stem = Path.GetFileNameWithoutExtension(basePath);
        var ext = Path.GetExtension(basePath);
        if (string.IsNullOrEmpty(ext)) ext = ".pcapng";
        if (string.IsNullOrEmpty(stem)) stem = "capture";
        return Path.Combine(dir, $"{stem}-{index:D2}-{Slug(title)}{ext}");
    }

    private static string Slug(string title)
    {
        var sb = new System.Text.StringBuilder(title.Length);
        bool lastDash = true;
        foreach (var c in title.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastDash = false;
            }
            else if (!lastDash)
            {
                sb.Append('-');
                lastDash = true;
            }
        }
        return sb.ToString().Trim('-');
    }

    private static int PrintUsage(int exitCode, string? error = null)
    {
        if (error is not null)
            Console.Error.WriteLine(error);
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  dotnet run                                # default: connect to pagila and emit a learning-tour capture");
        Console.Error.WriteLine("  dotnet run -- generate                    # same as default");
        Console.Error.WriteLine("  dotnet run -- capture-and-generate [out]  # capture traffic while generating; out defaults to pagila.pcapng");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Connection settings (libpq env vars, all optional):");
        Console.Error.WriteLine("  PGHOST (localhost) PGPORT (5432) PGUSER (postgres) PGPASSWORD (postgres)");
        Console.Error.WriteLine("  PGDATABASE (pagila) PGSSLMODE (Disable)");
        Console.Error.WriteLine();
        Console.Error.WriteLine("'capture-and-generate' needs packet-capture privileges:");
        Console.Error.WriteLine("  Windows: install Npcap (https://npcap.com)");
        Console.Error.WriteLine("  Linux/Mac: run as root or grant CAP_NET_RAW");
        return exitCode;
    }
}

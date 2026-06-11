using Microsoft.Extensions.DependencyInjection;
using pg_protoexport.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace pg_protoexport;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            // Build the DI graph once and reuse it for both the command registrar and
            // exporter-module discovery, so the service collection is never built twice.
            var services = BuildServiceCollection(AnsiConsole.Console);

            var app = new CommandApp(BuildTypeRegistrar(services));
            app.Configure(config => ConfigurePgProtoExport(services, config));
            return app.Run(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return -1;
        }
    }

    public static IServiceCollection BuildServiceCollection(IAnsiConsole console) =>
        new ServiceCollection()
            .AddSingleton(console)
            .AddLogging(configure => configure.AddSpectreConsole(console))
            .AddPcapService()
            .AddPcapPortDetector()
            .AddLiveCapture()
            .AddLatexExporter()
            .AddPqTraceExporter()
            .AddMermaidExporter()
            .AddPlantUmlExporter()
            .AddHtmlExporter()
            .AddAsciiExporter()
            .AddSingleton<IExportApp, ExportApp>();

    public static TypeRegistrar BuildTypeRegistrar(IServiceCollection services) =>
        new TypeRegistrar(services);

    // Host-level commands are registered here; every exporter contributes its own command(s)
    // and batch variants through an IExporterCliModule, so adding an exporter never touches this file.
    public static void ConfigurePgProtoExport(IServiceCollection services, IConfigurator config)
    {
        config.SetApplicationName("pg_protoexport");
        config.SetApplicationVersion(VersionInfo.Informational);

        config.AddCommand<VersionCommand>("version")
            .WithDescription("Print the pg_protoexport version and runtime info, then exit.")
            .WithExample("version");

        config.AddCommand<DemoCommand>("demo")
            .WithDescription("Interactive guided tour: walks through every command (starting with ascii) and can run each against a bundled sample capture.")
            .WithExample("demo")
            .WithExample("demo", "--no-run");

        config.AddCommand<CaptureCommand>("capture")
            .WithDescription("Record a .pcapng from a live NIC in-process (SharpPcap; no external tcpdump). Use --list-devices to enumerate adapters.")
            .WithExample("capture", "out.pcapng")
            .WithExample("capture", "out.pcapng", "--host", "localhost", "--port", "5432", "--duration", "30s")
            .WithExample("capture", "--list-devices");

        config.AddCommand<BatchExportCommand>("batchexport")
            .WithDescription("Run every exporter and every variant over every .pcapng / .pcap in a directory.")
            .WithExample("batchexport", "docs/examples/captures")
            .WithExample("batchexport", "docs/examples/captures", "docs/examples/exports")
            .WithExample("batchexport", "docs/examples/captures", "docs/examples/exports", "--port", "5432");

        foreach (var module in DiscoverCliModules(services))
            module.Register(config);

#if DEBUG
        config.ValidateExamples();
#endif
    }

    // Each exporter's Add{Format}Exporter() registers its IExporterCliModule. The modules are
    // stateless shims with parameterless constructors, so we read the registrations off the
    // already-built service collection and instantiate them directly rather than standing up a
    // throwaway ServiceProvider just to enumerate them.
    private static IReadOnlyList<IExporterCliModule> DiscoverCliModules(IServiceCollection services) =>
        services
            .Where(d => d.ServiceType == typeof(IExporterCliModule) && d.ImplementationType is not null)
            .Select(d => (IExporterCliModule)Activator.CreateInstance(d.ImplementationType!)!)
            .ToList();
}

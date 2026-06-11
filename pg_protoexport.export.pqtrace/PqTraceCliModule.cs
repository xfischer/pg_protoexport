using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class PqTraceCliModule : IExporterCliModule
{
    public void Register(IConfigurator config) =>
        config.AddCommand<ExportPqTraceCommand>("pqtrace")
            .WithDescription("Generate PQTrace-style tab-separated text.")
            .WithExample("pqtrace", "file.pcapng", "output.txt");

    public IEnumerable<BatchVariant> BatchVariants =>
    [
        new("pqtrace", null, null, "capture.pqtrace.txt"),
    ];
}

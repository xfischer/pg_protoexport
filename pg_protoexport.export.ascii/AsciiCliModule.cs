using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class AsciiCliModule : IExporterCliModule
{
    public void Register(IConfigurator config) =>
        config.AddCommand<ExportAsciiCommand>("ascii")
            .WithExample("ascii", "file.pcapng", "diagram.txt");

    public IEnumerable<BatchVariant> BatchVariants =>
    [
        new("ascii", null, null, "capture.ascii.txt"),
    ];
}

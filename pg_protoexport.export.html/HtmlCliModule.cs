using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class HtmlCliModule : IExporterCliModule
{
    public void Register(IConfigurator config) =>
        config.AddCommand<ExportHtmlCommand>("html")
            .WithExample("html", "file.pcapng", "report.html");

    public IEnumerable<BatchVariant> BatchVariants =>
    [
        new("html", null, null, "capture.html"),
    ];
}

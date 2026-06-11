using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class HtmlCliModule : IExporterCliModule
{
    public void Register(IConfigurator config) =>
        config.AddCommand<ExportHtmlCommand>("html")
            .WithDescription("Generate a self-contained, guided-reading HTML report.")
            .WithExample("html", "file.pcapng", "report.html");

    public IEnumerable<BatchVariant> BatchVariants =>
    [
        new("html", null, null, "capture.html"),
    ];
}

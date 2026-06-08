using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class LatexCliModule : IExporterCliModule
{
    public void Register(IConfigurator config) =>
        config.AddCommand<ExportLatexCommand>("latex")
            .WithExample("latex", "file.pcapng", "diagram.tex")
            .WithExample("latex", "file.pcapng", "diagram.tex", "--port", "5432", "--standalone", "--multiple");

    public IEnumerable<BatchVariant> BatchVariants =>
    [
        new("latex", null, new LatexExportOptions(Standalone: true, MultipleFiles: false, Render: null), "capture.tex"),
    ];
}

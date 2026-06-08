using Spectre.Console.Cli;

namespace pg_protoexport;

public class ExportLatexCommand(IExportApp app) : Command<LatexSettings>
{
    protected override int Execute(CommandContext context, LatexSettings settings, CancellationToken cancellation)
    {
        var render = new LatexRenderOptions
        {
            Exact = settings.Exact ?? false,
            RowWidthBytes = settings.RowWidthBytes ?? 32,
        };

        var opts = new LatexExportOptions(
            Standalone: settings.Standalone ?? true,
            MultipleFiles: settings.Multiple ?? false,
            Render: render);

        app.RunExport("latex", settings.InputFile, settings.OutputPath!, settings.Port, options: opts);

        return 0;
    }
}

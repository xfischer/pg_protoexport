using Spectre.Console.Cli;

namespace pg_protoexport;

public class ExportHtmlCommand(IExportApp app) : Command<HtmlSettings>
{
    protected override int Execute(CommandContext context, HtmlSettings settings, CancellationToken cancellation)
    {
        app.RunExport("html", settings.InputFile, settings.OutputPath!, settings.Port);
        return 0;
    }
}

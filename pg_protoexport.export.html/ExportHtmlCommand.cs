using Spectre.Console.Cli;

namespace pg_protoexport;

public class ExportHtmlCommand(IExportApp app) : Command<HtmlSettings>
{
    protected override int Execute(CommandContext context, HtmlSettings settings, CancellationToken cancellation)
    {
        var opts = new HtmlExportOptions(ShowTimeline: settings.Timeline ?? false);
        app.RunExport("html", settings.InputFile, settings.OutputPath!, settings.Port, options: opts);
        return 0;
    }
}

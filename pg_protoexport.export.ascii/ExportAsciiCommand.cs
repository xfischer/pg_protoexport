using Spectre.Console.Cli;

namespace pg_protoexport;

public class ExportAsciiCommand(IExportApp app) : Command<AsciiSettings>
{
    protected override int Execute(CommandContext context, AsciiSettings settings, CancellationToken cancellation)
    {
        var opts = new AsciiExportOptions(MaxLineWidth: settings.MaxWidth);
        app.RunExport("ascii", settings.InputFile, settings.OutputPath!, settings.Port, options: opts);
        return 0;
    }
}

using Spectre.Console.Cli;

namespace pg_protoexport;

public class ExportPqTraceCommand(IExportApp app) : Command<PqTraceSettings>
{
    protected override int Execute(CommandContext context, PqTraceSettings settings, CancellationToken cancellation)
    {
        app.RunExport("pqtrace", settings.InputFile, settings.OutputPath!, settings.Port);

        return 0;
    }
}

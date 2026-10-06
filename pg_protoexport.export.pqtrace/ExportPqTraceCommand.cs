using Spectre.Console.Cli;

namespace pg_protoexport;

public class ExportPqTraceCommand(IExportApp app) : Command<PqTraceSettings>
{
    public override int Execute(CommandContext context, PqTraceSettings settings, CancellationToken cancellation)
    {
        app.RunExport("pqtrace", settings.InputFile, settings.OutputPath!, settings.Port);

        return 0;
    }
}

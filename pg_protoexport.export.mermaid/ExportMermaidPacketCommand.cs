using Spectre.Console.Cli;

namespace pg_protoexport;

public class ExportMermaidPacketCommand(IExportApp app) : Command<MermaidSettings>
{
    protected override int Execute(CommandContext context, MermaidSettings settings, CancellationToken cancellation)
    {
        app.RunExport("mermaid", settings.InputFile, settings.OutputPath!, settings.Port, mode: PcapToMermaidService.ModePacket);

        return 0;
    }
}

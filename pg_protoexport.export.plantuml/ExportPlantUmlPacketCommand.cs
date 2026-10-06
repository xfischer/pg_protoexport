using Spectre.Console.Cli;

namespace pg_protoexport;

public class ExportPlantUmlPacketCommand(IExportApp app) : Command<PlantUmlSettings>
{
    public override int Execute(CommandContext context, PlantUmlSettings settings, CancellationToken cancellation)
    {
        app.RunExport("plantuml", settings.InputFile, settings.OutputPath!, settings.Port, mode: PcapToPlantUmlService.ModePacket);

        return 0;
    }
}

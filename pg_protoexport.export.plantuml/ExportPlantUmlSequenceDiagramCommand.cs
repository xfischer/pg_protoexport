using Spectre.Console.Cli;

namespace pg_protoexport;

public class ExportPlantUmlSequenceDiagramCommand(IExportApp app) : Command<PlantUmlSettings>
{
    protected override int Execute(CommandContext context, PlantUmlSettings settings, CancellationToken cancellation)
    {
        app.RunExport("plantuml", settings.InputFile, settings.OutputPath!, settings.Port, mode: PcapToPlantUmlService.ModeSequenceDiagram);

        return 0;
    }
}

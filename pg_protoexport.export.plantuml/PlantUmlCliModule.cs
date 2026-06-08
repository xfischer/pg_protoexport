using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class PlantUmlCliModule : IExporterCliModule
{
    public void Register(IConfigurator config) =>
        config.AddBranch<ExportSettings>("plantuml", plantuml =>
        {
            plantuml.AddCommand<ExportPlantUmlSequenceDiagramCommand>("sequenceDiagram")
                .WithExample("plantuml", "file.pcapng", "sequenceDiagram", "diagram.md");

            plantuml.AddCommand<ExportPlantUmlPacketCommand>("packet")
                .WithExample("plantuml", "file.pcapng", "packet", "diagram.md");
        });

    public IEnumerable<BatchVariant> BatchVariants =>
    [
        new("plantuml", PcapToPlantUmlService.ModeSequenceDiagram, null, "capture.plantuml.seq.md"),
        new("plantuml", PcapToPlantUmlService.ModePacket,          null, "capture.plantuml.pkt.md"),
    ];
}

using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class PlantUmlCliModule : IExporterCliModule
{
    public void Register(IConfigurator config) =>
        config.AddBranch<ExportSettings>("plantuml", plantuml =>
        {
            plantuml.SetDescription("Markdown with embedded PlantUML diagrams (sequence or packet).");

            plantuml.AddCommand<ExportPlantUmlSequenceDiagramCommand>("sequenceDiagram")
                .WithDescription("@startuml sequence diagram of the client/server flow.")
                .WithExample("plantuml", "file.pcapng", "sequenceDiagram", "diagram.md");

            plantuml.AddCommand<ExportPlantUmlPacketCommand>("packet")
                .WithDescription("@startjson per-message field tree.")
                .WithExample("plantuml", "file.pcapng", "packet", "diagram.md");
        });

    public IEnumerable<BatchVariant> BatchVariants =>
    [
        new("plantuml", PcapToPlantUmlService.ModeSequenceDiagram, null, "capture.plantuml.seq.md"),
        new("plantuml", PcapToPlantUmlService.ModePacket,          null, "capture.plantuml.pkt.md"),
    ];
}

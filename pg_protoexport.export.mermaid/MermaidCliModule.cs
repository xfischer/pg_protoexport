using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class MermaidCliModule : IExporterCliModule
{
    public void Register(IConfigurator config) =>
        config.AddBranch<ExportSettings>("mermaid", mermaid =>
        {
            mermaid.AddCommand<ExportMermaidSequenceDiagramCommand>("sequenceDiagram")
                .WithExample("mermaid", "file.pcapng", "sequenceDiagram", "diagram.md");

            mermaid.AddCommand<ExportMermaidPacketCommand>("packet")
                .WithExample("mermaid", "file.pcapng", "packet", "diagram.md");
        });

    public IEnumerable<BatchVariant> BatchVariants =>
    [
        new("mermaid", PcapToMermaidService.ModeSequenceDiagram, null, "capture.mermaid.seq.md"),
        new("mermaid", PcapToMermaidService.ModePacket,          null, "capture.mermaid.pkt.md"),
    ];
}

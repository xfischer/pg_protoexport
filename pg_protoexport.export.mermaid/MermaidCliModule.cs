using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class MermaidCliModule : IExporterCliModule
{
    public void Register(IConfigurator config) =>
        config.AddBranch<ExportSettings>("mermaid", mermaid =>
        {
            mermaid.SetDescription("Markdown with embedded Mermaid diagrams (sequenceDiagram or packet).");

            mermaid.AddCommand<ExportMermaidSequenceDiagramCommand>("sequenceDiagram")
                .WithDescription("Sequence diagram of the client/server message flow.")
                .WithExample("mermaid", "file.pcapng", "sequenceDiagram", "diagram.md");

            mermaid.AddCommand<ExportMermaidPacketCommand>("packet")
                .WithDescription("Byte-accurate packet diagram, one block per message.")
                .WithExample("mermaid", "file.pcapng", "packet", "diagram.md");
        });

    public IEnumerable<BatchVariant> BatchVariants =>
    [
        new("mermaid", PcapToMermaidService.ModeSequenceDiagram, null, "capture.mermaid.seq.md"),
        new("mermaid", PcapToMermaidService.ModePacket,          null, "capture.mermaid.pkt.md"),
    ];
}

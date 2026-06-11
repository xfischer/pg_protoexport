using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class AsciiCliModule : IExporterCliModule
{
    public void Register(IConfigurator config) =>
        config.AddBranch<AsciiBranchSettings>("ascii", ascii =>
        {
            // --console / --max-width are branch options, so they must precede the mode keyword
            // (e.g. `ascii file.pcapng --console sequenceDiagram`); the examples model that placement.
            ascii.AddCommand<ExportAsciiFieldsCommand>("fields")
                .WithDescription("Render each message as labelled, content-sized field boxes. Pass --console / --max-width before the mode.")
                .WithExample("ascii", "file.pcapng", "fields", "diagram.txt")
                .WithExample("ascii", "file.pcapng", "--console", "fields");

            ascii.AddCommand<ExportAsciiSequenceDiagramCommand>("sequenceDiagram")
                .WithDescription("Render a two-lifeline (Client/Server) sequence diagram. Pass --console / --max-width before the mode.")
                .WithExample("ascii", "file.pcapng", "sequenceDiagram", "diagram.txt")
                .WithExample("ascii", "file.pcapng", "--console", "sequenceDiagram");
        });

    public IEnumerable<BatchVariant> BatchVariants =>
    [
        new("ascii", PcapToAsciiService.ModeFields,          null, "capture.ascii.txt"),
        new("ascii", PcapToAsciiService.ModeSequenceDiagram, null, "capture.ascii.seq.txt"),
    ];
}

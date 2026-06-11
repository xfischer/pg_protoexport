using Spectre.Console.Cli;

namespace pg_protoexport;

/// <summary>Renders each message as a row of labelled, content-sized boxes (the default mode).</summary>
public class ExportAsciiFieldsCommand(IExportApp app) : Command<AsciiSettings>
{
    protected override int Execute(CommandContext context, AsciiSettings settings, CancellationToken cancellation)
        => RunAscii(app, settings, PcapToAsciiService.ModeFields);

    internal static int RunAscii(IExportApp app, AsciiSettings settings, string mode)
    {
        var opts = new AsciiExportOptions(MaxLineWidth: settings.MaxWidth, ToConsole: settings.Console);
        var outputPath = settings.Console ? "" : settings.OutputPath!;
        app.RunExport("ascii", settings.InputFile, outputPath, settings.Port, mode: mode, options: opts);
        return 0;
    }
}

/// <summary>Renders the capture as a two-lifeline ASCII sequence diagram, one arrow per packet.</summary>
public class ExportAsciiSequenceDiagramCommand(IExportApp app) : Command<AsciiSettings>
{
    protected override int Execute(CommandContext context, AsciiSettings settings, CancellationToken cancellation)
        => ExportAsciiFieldsCommand.RunAscii(app, settings, PcapToAsciiService.ModeSequenceDiagram);
}

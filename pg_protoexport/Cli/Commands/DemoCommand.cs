using Spectre.Console;
using Spectre.Console.Cli;

namespace pg_protoexport;

/// <summary>
/// Interactive guided tour of the CLI. Walks through every command (starting with <c>ascii</c>),
/// explaining each and showing the exact line to type; in an interactive terminal it can run each
/// against a bundled sample capture. When output is piped / non-interactive — or <c>--no-run</c> is
/// passed — it just prints the whole tour with no prompts and no execution.
/// </summary>
public class DemoCommand(IAnsiConsole console, IExportApp app) : Command<DemoSettings>
{
    private sealed record Lesson(string Heading, string Description, string Command, Action? Run);

    protected override int Execute(CommandContext context, DemoSettings settings, CancellationToken cancellation)
    {
        string? sample = ResolveSample();
        bool interactive = !settings.NoRun
            && !Console.IsInputRedirected
            && console.Profile.Capabilities.Interactive;
        bool canRun = interactive && sample is not null;

        var tempDir = Path.Combine(Path.GetTempPath(), "pg_protoexport-demo");
        string sampleName = sample is null ? "capture.pcapng" : Path.GetFileName(sample);

        console.Write(new Panel(
                "[bold]pg_protoexport[/] — interactive tour\n\n" +
                "A quick walk through every command, starting with [bold]ascii[/]. " +
                "Each step shows the command to type; in an interactive terminal you can run it live.")
            .Header("demo")
            .RoundedBorder());

        if (sample is null)
            console.MarkupLine("[yellow]No bundled sample capture found — showing commands only (nothing will run).[/]");
        else
            console.MarkupLine($"Sample capture: [bold]{Markup.Escape(sample)}[/]");
        console.WriteLine();

        foreach (var lesson in BuildLessons(sample, sampleName, tempDir, canRun))
        {
            console.Write(new Rule($"[yellow]{Markup.Escape(lesson.Heading)}[/]").LeftJustified());
            console.MarkupLine(Markup.Escape(lesson.Description));
            console.MarkupLine($"  [grey]Try:[/] [bold]{Markup.Escape(lesson.Command)}[/]");

            if (canRun && lesson.Run is not null && console.Confirm("Run it now?"))
            {
                try
                {
                    Directory.CreateDirectory(tempDir);
                    lesson.Run();
                }
                catch (Exception ex)
                {
                    console.MarkupLine($"[red]Run failed:[/] {Markup.Escape(ex.Message)}");
                }
            }

            if (interactive)
            {
                var answer = console.Prompt(
                    new TextPrompt<string>("[grey](enter for next, q to quit)[/]").AllowEmpty());
                if (answer.Trim().Equals("q", StringComparison.OrdinalIgnoreCase))
                    break;
            }
            else
            {
                console.WriteLine();
            }
        }

        console.Write(new Rule().RoundedBorder());
        console.MarkupLine("Tour complete. Run [bold]pg_protoexport --help[/] for the full command list, " +
                           "or [bold]pg_protoexport <command> --help[/] for any command's options.");
        return 0;
    }

    private List<Lesson> BuildLessons(string? sample, string sampleName, string tempDir, bool canRun)
    {
        // ascii diagrams render straight to the console via the --console feature; the other
        // exporters write to a temp file we preview inline.
        Action? AsciiRun(string mode) =>
            canRun ? () => app.RunExport("ascii", sample!, "", port: null, mode: mode,
                                         options: new AsciiExportOptions(ToConsole: true)) : null;

        Action? FileRun(string exporter, string? mode, string fileName) =>
            canRun ? () => RunToFileAndShow(sample!, exporter, mode, Path.Combine(tempDir, fileName)) : null;

        return
        [
            new("ascii sequenceDiagram",
                "Render the conversation as ASCII lifelines (Client / Server) with one arrow per packet.",
                $"pg_protoexport ascii {sampleName} --console sequenceDiagram",
                AsciiRun(PcapToAsciiService.ModeSequenceDiagram)),

            new("ascii fields",
                "Render every message as labelled, content-sized field boxes.",
                $"pg_protoexport ascii {sampleName} --console fields",
                AsciiRun(PcapToAsciiService.ModeFields)),

            new("mermaid",
                "Markdown with embedded Mermaid diagrams (sequenceDiagram or packet).",
                $"pg_protoexport mermaid {sampleName} sequenceDiagram diagram.md",
                FileRun("mermaid", PcapToMermaidService.ModeSequenceDiagram, "capture.mermaid.md")),

            new("plantuml",
                "Markdown with embedded PlantUML diagrams (sequence or packet).",
                $"pg_protoexport plantuml {sampleName} sequenceDiagram diagram.md",
                FileRun("plantuml", PcapToPlantUmlService.ModeSequenceDiagram, "capture.plantuml.md")),

            new("html",
                "A self-contained, guided-reading HTML report (open it in a browser).",
                $"pg_protoexport html {sampleName} report.html",
                FileRun("html", null, "capture.html")),

            new("latex",
                "LaTeX bytefield diagrams (standalone documents or a paged article).",
                $"pg_protoexport latex {sampleName} diagram.tex",
                FileRun("latex", null, "capture.tex")),

            new("pqtrace",
                "PQTrace-style tab-separated text.",
                $"pg_protoexport pqtrace {sampleName} output.txt",
                FileRun("pqtrace", null, "capture.pqtrace.txt")),

            new("capture",
                "Record a .pcapng from a live NIC in-process (needs Npcap/libpcap). Use --list-devices to enumerate adapters.",
                "pg_protoexport capture out.pcapng --host localhost --port 5432 --duration 30s",
                Run: null),

            new("batchexport",
                "Run every exporter and variant over every .pcapng / .pcap in a directory.",
                "pg_protoexport batchexport docs/examples/captures docs/examples/exports",
                Run: null),
        ];
    }

    private void RunToFileAndShow(string sample, string exporter, string? mode, string outPath)
    {
        app.RunExport(exporter, sample, outPath, port: null, mode: mode);

        if (exporter == "html")
        {
            console.MarkupLine($"[green]Wrote[/] {Markup.Escape(outPath)} (+ a sibling _assets/ folder) — open it in a browser.");
            return;
        }

        if (!File.Exists(outPath))
            return;

        var lines = File.ReadLines(outPath).Take(25).ToList();
        console.Write(new Panel(new Text(string.Join(Environment.NewLine, lines)))
            .Header($"{Path.GetFileName(outPath)} (first {lines.Count} lines)")
            .RoundedBorder());
    }

    private static string? ResolveSample()
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "SampleData", "extendedQuery.pcapng");
        if (File.Exists(bundled))
            return bundled;

        var fromCwd = Path.Combine("docs", "examples", "captures", "extendedQuery.pcapng");
        return File.Exists(fromCwd) ? Path.GetFullPath(fromCwd) : null;
    }
}

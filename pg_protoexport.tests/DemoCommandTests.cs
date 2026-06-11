using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace pg_protoexport.tests;

// The demo command is an interactive tour, but a TestConsole is non-interactive by default, so
// running it here exercises the "print the whole tour, prompt/run nothing" fallback path.
public class DemoCommandTests
{
    private static CommandAppTester CreateApp()
    {
        // A real terminal reports a bounded width; TestConsole defaults to an effectively unbounded
        // one, which makes full-width renderables (Rule) allocate a huge buffer. Pin a sane size and
        // hand the same console to the tester so it's used by both the harness and the command.
        var console = new TestConsole().Width(120);
        var services = Program.BuildServiceCollection(console);
        var app = new CommandAppTester(Program.BuildTypeRegistrar(services), new CommandAppTesterSettings(), console);
        app.Configure(config => Program.ConfigurePgProtoExport(services, config));
        return app;
    }

    [Fact]
    public void Demo_NonInteractive_PrintsEveryCommand_StartingWithAscii()
    {
        var result = CreateApp().Run("demo", "--no-run");
        var output = result.Output;

        Assert.Equal(0, result.ExitCode);

        // Every command surfaces in the tour.
        foreach (var name in new[] { "ascii sequenceDiagram", "ascii fields", "mermaid", "plantuml", "html", "latex", "pqtrace", "capture", "batchexport" })
            Assert.Contains(name, output);

        // ascii comes first, before the other formats.
        Assert.True(output.IndexOf("ascii sequenceDiagram", StringComparison.Ordinal)
            < output.IndexOf("plantuml", StringComparison.Ordinal));
    }

    [Fact]
    public void Demo_NonInteractive_ShowsCommandLinesToType()
    {
        var result = CreateApp().Run("demo", "--no-run");

        Assert.Equal(0, result.ExitCode);
        // The tour shows the exact CLI line for the ascii console feature.
        Assert.Contains("pg_protoexport ascii", result.Output);
        Assert.Contains("--console sequenceDiagram", result.Output);
    }

    [Fact]
    public void Help_ListsDescriptions_ForHostCommandsAndBranches()
    {
        var result = CreateApp().Run("--help");

        Assert.Equal(0, result.ExitCode);
        // Previously-undocumented commands now carry a description.
        Assert.Contains("Interactive guided tour", result.Output);   // demo
        Assert.Contains("Record a .pcapng", result.Output);          // capture
        Assert.Contains("PQTrace-style", result.Output);             // pqtrace
        Assert.Contains("Mermaid diagrams", result.Output);          // mermaid branch
    }
}

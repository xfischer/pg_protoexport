using System.Text.RegularExpressions;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace pg_protoexport.tests;

public class VersionCommandTests
{
    private static CommandAppTester CreateApp()
    {
        var console = new TestConsole().Width(120);
        var services = Program.BuildServiceCollection(console);
        var app = new CommandAppTester(Program.BuildTypeRegistrar(services), new CommandAppTesterSettings(), console);
        app.Configure(config => Program.ConfigurePgProtoExport(services, config));
        return app;
    }

    [Fact]
    public void Version_Command_PrintsNameAndVersion()
    {
        var result = CreateApp().Run("version");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("pg_protoexport", result.Output);
        Assert.Matches(@"\d+\.\d+", result.Output);
    }

    [Fact]
    public void VersionFlag_PrintsVersion()
    {
        var result = CreateApp().Run("--version");

        Assert.Equal(0, result.ExitCode);
        Assert.Matches(@"\d+\.\d+", result.Output);
    }
}

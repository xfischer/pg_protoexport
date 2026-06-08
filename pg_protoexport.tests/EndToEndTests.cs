using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
namespace pg_protoexport.tests;

// docs: https://spectreconsole.net/cli/unit-testing

[CollectionDefinition(nameof(CommandLine_EndToEndTests), DisableParallelization = true)]
public class CommandLine_EndToEndTests
{
    private (TestConsole Console, CommandAppTester App) CreateTestApp()
    {
        var console = new TestConsole();
        var services = Program.BuildServiceCollection(console);
        var app = new CommandAppTester(Program.BuildTypeRegistrar(services));
        app.Configure(config => Program.ConfigurePgProtoExport(services, config));
        return (console, app);
    }

    [Fact]
    public void CommandLine_EmptyArgs()
    {
        // arrange
        var app = new CommandAppTester();

        // act
        var result = app.Run();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("USAGE:", result.Output);
    }

    [Fact]
    public void CommandLine_ValidFile()
    {
        // arrange
        var testApp = CreateTestApp();
        string[] args = ["latex", "TestData/extendedQuery.pcapng", "5432"];

        // act
        var result = testApp.App.Run(args);

        // assert
        Assert.Equal(0, result.ExitCode);        
        Assert.EndsWith("3 packet(s) processed. 21 messages written.\n", testApp.Console.Output);
    }


    [Fact]
    public void CommandLine_ValidFile_InvalidPort()
    {
        // arrange — port is now an option (--port); a non-matching value should yield 0 packets.
        var testApp = CreateTestApp();
        string[] args = ["latex", "TestData/extendedQuery.pcapng", "--port", "1000"];

        // act
        var result = testApp.App.Run(args);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("0 packet(s) processed. 0 messages written.\n", testApp.Console.Output);
    }

    [Fact]
    public void CommandLine_MermaidSequenceDiagram_ValidFile()
    {
        // arrange — port arg dropped; auto-detection picks 5432 from the SYN handshake.
        var testApp = CreateTestApp();
        string[] args = ["mermaid", "TestData/extendedQuery.pcapng", "sequenceDiagram"];

        // act
        var result = testApp.App.Run(args);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Wrote 2 sequence diagram(s)", testApp.Console.Output);
    }

    [Fact]
    public void CommandLine_MermaidPacket_ValidFile()
    {
        // arrange — port arg dropped; auto-detection picks 5432 from the SYN handshake.
        var testApp = CreateTestApp();
        string[] args = ["mermaid", "TestData/extendedQuery.pcapng", "packet"];

        // act
        var result = testApp.App.Run(args);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Wrote 3 packet diagram(s)", testApp.Console.Output);
    }

    [Fact]
    public void CommandLine_Html_ValidFile()
    {
        // arrange
        var testApp = CreateTestApp();
        string[] args = ["html", "TestData/extendedQuery.pcapng", "5432"];

        // act
        var result = testApp.App.Run(args);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Wrote HTML report to", testApp.Console.Output);
    }

    [Fact]
    public void CommandLine_InvalidFile()
    {
        // arrange
        var testApp = CreateTestApp();
        string[] args = ["latex", "TestData/DOES_NOT_EXISTS.pcapng", "5432"];

        // act
        var result = testApp.App.Run(args);

        // assert
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("Error: Input file TestData/DOES_NOT_EXISTS.pcapng does not exists.", result.Output);
    }
}

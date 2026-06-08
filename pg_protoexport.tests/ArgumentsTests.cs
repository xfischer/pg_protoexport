namespace pg_protoexport.tests;

public class ArgumentsTests
{
    [Fact]
    public void Execute_WithValidFile()
    {
        // arrange
        var settings = new LatexSettings() {
            InputFile = "TestData/extendedQuery.pcapng",
            Multiple = false,
            Port = 5446 };

        // act
        var result = settings.Validate();

        // assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Execute_WithValidFile_MermaidSettings()
    {
        // arrange
        var settings = new MermaidSettings() {
            InputFile = "TestData/extendedQuery.pcapng",
            Port = 5432 };

        // act
        var result = settings.Validate();

        // assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Execute_MermaidSettings_DefaultOutputPath_HasMdExtension()
    {
        // arrange
        var settings = new MermaidSettings() {
            InputFile = "TestData/extendedQuery.pcapng",
            Port = 5432 };

        // act
        settings.Validate();

        // assert
        Assert.EndsWith(".md", settings.OutputPath);
    }
}


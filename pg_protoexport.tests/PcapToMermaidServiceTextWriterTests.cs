using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace pg_protoexport.tests;

public class PcapToMermaidServiceTextWriterTests : IDisposable
{
    private readonly string _tempDir;

    public PcapToMermaidServiceTextWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"mermaid_tw_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static IEnumerable<PostgresPacket> GetTestPackets()
    {
        var pcapOptions = new PcapPostgresOptions();
        pcapOptions.AddDefaultPostgresMessages();
        var pcapService = new PcapService(NullLogger<PcapService>.Instance, Options.Create(pcapOptions));
        return pcapService.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 5432);
    }

    [Fact]
    public void TextWriterOverload_ProducesSameContentAsFilePathOverload()
    {
        // arrange — materialize twice to feed each call independently
        var packetsForFile = GetTestPackets().ToList();
        var packetsForWriter = GetTestPackets().ToList();
        var service = new PcapToMermaidService(NullLogger<PcapToMermaidService>.Instance);

        var outputFile = Path.Combine(_tempDir, "sequence.md");

        // act
        service.PcapToSequenceDiagram(packetsForFile, outputFile);
        var fileContent = File.ReadAllText(outputFile);

        var writer = new StringWriter();
        service.PcapToSequenceDiagram(packetsForWriter, writer);
        var writerContent = writer.ToString();

        // assert
        Assert.Equal(fileContent, writerContent);
    }
}

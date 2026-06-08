using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace pg_protoexport.tests;

public class PcapToPlantUmlTests : IDisposable
{
    private readonly string _tempDir;

    public PcapToPlantUmlTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"plantuml_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private IEnumerable<PostgresPacket> GetTestPackets()
    {
        var pcapOptions = new PcapPostgresOptions();
        pcapOptions.AddDefaultPostgresMessages();
        PcapService pcapService = new PcapService(NullLogger<PcapService>.Instance, Options.Create(pcapOptions));
        return pcapService.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 5432);
    }

    private PcapToPlantUmlService CreateService()
    {
        return new PcapToPlantUmlService(NullLogger<PcapToPlantUmlService>.Instance);
    }

    private string RunSequenceDiagram()
    {
        var packets = GetTestPackets();
        var service = CreateService();
        var outputFile = Path.Combine(_tempDir, "sequence.md");

        service.PcapToSequenceDiagram(packets, outputFile);
        return File.ReadAllText(outputFile);
    }

    private string RunPacketDiagram()
    {
        var packets = GetTestPackets();
        var service = CreateService();
        var outputFile = Path.Combine(_tempDir, "packet.md");

        service.PcapToPacketDiagram(packets, outputFile);
        return File.ReadAllText(outputFile);
    }

    // ── Sequence diagram tests ──────────────────────────────────────────

    [Fact]
    public void SequenceDiagram_GeneratesValidPlantUmlSyntax()
    {
        // act
        var content = RunSequenceDiagram();

        // assert
        Assert.Contains("```plantuml", content);
        Assert.Contains("@startuml", content);
        Assert.Contains("@enduml", content);
        Assert.Contains("participant \"Client", content);
        Assert.Contains("participant \"Server", content);
    }

    [Fact]
    public void SequenceDiagram_SplitsAtReadyForQuery()
    {
        // The test data has ReadyForQuery in packet 2, then Terminate in packet 3.
        // This should produce 2 separate diagrams.

        // act
        var content = RunSequenceDiagram();

        // assert
        var diagramCount = content.Split("@startuml").Length - 1;
        Assert.Equal(2, diagramCount);
    }

    [Fact]
    public void SequenceDiagram_ContainsFrontendMessages()
    {
        // act
        var content = RunSequenceDiagram();

        // assert — frontend messages from packet 1, merged onto one line
        Assert.Contains("C -> S : Parse / Bind / Describe / Execute / Sync", content);
    }

    [Fact]
    public void SequenceDiagram_ContainsBackendMessages()
    {
        // act
        var content = RunSequenceDiagram();

        // assert — backend messages from packet 2, same-direction singles merged
        Assert.Contains("S -> C : ParseComplete / BindComplete / RowDescription", content);
        Assert.Contains("S -> C : DataRow (x10)", content);
        Assert.Contains("S -> C : CommandComplete", content);
        Assert.Contains("S -> C : ReadyForQuery", content);
    }

    [Fact]
    public void SequenceDiagram_GroupsConsecutiveDataRows()
    {
        // The test data has 10 consecutive DataRow messages in packet 2.

        // act
        var content = RunSequenceDiagram();

        // assert
        Assert.Contains("S -> C : DataRow (x10)", content);
    }

    [Fact]
    public void SequenceDiagram_ContainsTerminateInSeparateDiagram()
    {
        // Terminate should start its own diagram (after ReadyForQuery closed the first).

        // act
        var content = RunSequenceDiagram();

        // assert
        Assert.Contains("C -> S : Terminate", content);
    }

    [Fact]
    public void SequenceDiagram_AllDiagramsAreClosed()
    {
        // act
        var content = RunSequenceDiagram();
        var lines = content.Split('\n', StringSplitOptions.TrimEntries);

        // assert — every opened diagram should be closed
        var openFenceCount = lines.Count(l => l.StartsWith("```plantuml"));
        var closeFenceCount = lines.Count(l => l == "```");
        var startUmlCount = lines.Count(l => l == "@startuml");
        var endUmlCount = lines.Count(l => l == "@enduml");

        Assert.Equal(openFenceCount, closeFenceCount);
        Assert.Equal(startUmlCount, endUmlCount);
        Assert.Equal(openFenceCount, startUmlCount);
    }

    // ── Packet diagram tests ────────────────────────────────────────────

    [Fact]
    public void PacketDiagram_GeneratesValidPlantUmlSyntax()
    {
        // act
        var content = RunPacketDiagram();

        // assert
        Assert.Contains("```plantuml", content);
        Assert.Contains("@startjson", content);
        Assert.Contains("@endjson", content);
    }

    [Fact]
    public void PacketDiagram_GeneratesOnePerPacket()
    {
        // The test data has 3 packets.

        // act
        var content = RunPacketDiagram();

        // assert
        Assert.Contains("# Packet 1", content);
        Assert.Contains("# Packet 2", content);
        Assert.Contains("# Packet 3", content);
        Assert.DoesNotContain("# Packet 4", content);
    }

    [Fact]
    public void PacketDiagram_ShowsDirectionInHeaders()
    {
        // Packet 1 and 3 are frontend, packet 2 is backend.

        // act
        var content = RunPacketDiagram();

        // assert
        Assert.Contains("FrontEnd --> BackEnd", content);
        Assert.Contains("FrontEnd <-- BackEnd", content);
    }

    [Fact]
    public void PacketDiagram_ShowsMessageCounts()
    {
        // Packet 1: 5 messages, Packet 2: 15 messages, Packet 3: 1 message.

        // act
        var content = RunPacketDiagram();

        // assert
        Assert.Contains("5 messages", content);
        Assert.Contains("15 messages", content);
        Assert.Contains("1 messages", content);
    }

    [Fact]
    public void PacketDiagram_ContainsMessageCodeAndLength()
    {
        // act
        var content = RunPacketDiagram();

        // assert — Parse message has code 'P' and JSON contains Code/Length keys
        Assert.Contains("\"Code\":", content);
        Assert.Contains("\"Length\":", content);
        Assert.Contains("P (1 byte)", content);
    }

    [Fact]
    public void PacketDiagram_UsesJsonByteAnnotations()
    {
        // act
        var content = RunPacketDiagram();

        // assert — fields are annotated with their wire size
        Assert.Contains("(1 byte)", content);   // 1-byte fields (code, status)
        Assert.Contains("(4 bytes)", content);  // 4-byte fields (length, int32)
        Assert.Contains("(2 bytes)", content);  // 2-byte fields (int16)
    }

    [Fact]
    public void PacketDiagram_StringFieldsUseActualByteSize()
    {
        // "SELECT 10" = 9 chars + 1 null terminator = 10 bytes

        // act
        var content = RunPacketDiagram();

        // assert
        Assert.Contains("SELECT 10", content);
        Assert.Contains("(10 bytes)", content);
    }

    [Fact]
    public void PacketDiagram_GroupsConsecutiveDataRows()
    {
        // Packet 2 has 10 consecutive DataRow messages → JSON key shows count.

        // act
        var content = RunPacketDiagram();

        // assert
        Assert.Contains("\"DataRow (x10)\"", content);
    }

    [Fact]
    public void PacketDiagram_EachMessageHasJsonObject()
    {
        // act
        var content = RunPacketDiagram();

        // assert — message names appear as JSON keys
        Assert.Contains("\"Parse\":", content);
        Assert.Contains("\"Bind\":", content);
        Assert.Contains("\"Describe\":", content);
        Assert.Contains("\"Execute\":", content);
        Assert.Contains("\"Sync\":", content);
        Assert.Contains("\"Terminate\":", content);
    }

    // ── Factory method test ─────────────────────────────────────────────

    [Fact]
    public void Create_ReturnsWorkingService()
    {
        // act
        var service = PcapToPlantUmlService.Create();

        // assert
        Assert.NotNull(service);
        Assert.IsType<PcapToPlantUmlService>(service);
    }
}

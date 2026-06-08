using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace pg_protoexport.tests;

public class PcapToLatexTests
{

    private IEnumerable<PostgresPacket> GetTestPackets()
    {
        var pcapOptions = new PcapPostgresOptions();
        pcapOptions.AddDefaultPostgresMessages();
        PcapService pcapService = new PcapService(NullLogger<PcapService>.Instance, Options.Create(pcapOptions));
        return pcapService.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 5432);
    }

    [Fact]
    public void Convert_Standalone()
    {
        // arrange
        var packets = GetTestPackets();
        var options = new PcapToLatexOptions();
        PcapToLatexService service = new PcapToLatexService(NullLogger<PcapToLatexService>.Instance, Options.Create(options));


        // act
        using var memStream = new MemoryStream();
        var state = service.PcapToLaTeX(packets, memStream, standalone: true);
        memStream.Seek(0, SeekOrigin.Begin);
        using var streamReader = new StreamReader(memStream);
        var content = streamReader.ReadToEnd();

        // assert        
        Assert.Equal(3, state.StatsPacketsProcessed);
        Assert.Equal(21, state.StatsMessagesProcessed);

        Assert.Contains("documentclass[margin=8mm]{standalone}", content);
    }

    [Fact]
    public void Convert_Article()
    {
        // arrange
        var packets = GetTestPackets();
        var options = new PcapToLatexOptions();
        PcapToLatexService service = new PcapToLatexService(NullLogger<PcapToLatexService>.Instance, Options.Create(options));


        // act
        using var memStream = new MemoryStream();
        var state = service.PcapToLaTeX(packets, memStream, standalone: false);
        memStream.Seek(0, SeekOrigin.Begin);
        using var streamReader = new StreamReader(memStream);
        var content = streamReader.ReadToEnd();

        // assert
        Assert.Equal(3, state.StatsPacketsProcessed);
        Assert.Equal(21, state.StatsMessagesProcessed);

        Assert.Contains("documentclass{article}", content);
    }

    private static string RunExact(IEnumerable<PostgresPacket> packets, int rowWidthBytes = 32)
    {
        var options = new PcapToLatexOptions();
        var service = new PcapToLatexService(NullLogger<PcapToLatexService>.Instance, Options.Create(options));
        var render = new LatexRenderOptions { Exact = true, RowWidthBytes = rowWidthBytes };

        using var memStream = new MemoryStream();
        service.PcapToLaTeX(packets, memStream, standalone: false, render);
        memStream.Seek(0, SeekOrigin.Begin);
        return new StreamReader(memStream).ReadToEnd();
    }

    [Fact]
    public void Convert_Exact_LongQueryWraps()
    {
        // The Parse message in the test pcap has a ~70-byte query, so wrapping
        // happens at rowWidth=32 (it spans multiple \bitbox{32}{...} cells).
        var content = RunExact(GetTestPackets(), rowWidthBytes: 32);

        // A full-row chunk of the query payload is expected (32 raw bytes between bitbox braces).
        Assert.Matches(@"\\bitbox\{32\}\{[^\}]+\}", content);
        // No bitbox in exact mode should carry the old truncation marker.
        // (The marker still legitimately appears in the SkippedWords placeholder header text,
        // which is not a per-message bitbox content.)
        Assert.DoesNotMatch(@"\\bitbox\{\d+\}(?:\[[^\]]*\])?\{[^\}]*\\cdots", content);
    }

    [Fact]
    public void Convert_Exact_RowWidthOverride()
    {
        var content = RunExact(GetTestPackets(), rowWidthBytes: 64);

        // The bytefield declaration must use the override row width.
        Assert.Contains("\\begin{bytefield}[boxformatting={\\centering\\small}, bitheight=8ex]{64}", content);
        // The bitheader tick mark for the last column must reflect rowWidth-1.
        Assert.Contains("\\bitheader{0,1,4,5,63}", content);
    }

    [Fact]
    public void Convert_Exact_LiteralQueryBytesAppear()
    {
        var content = RunExact(GetTestPackets(), rowWidthBytes: 32);

        // The original SQL text must appear in the exact-mode output (no truncation).
        // The test pcap's Parse query is "select oid, typname, typtype from pg_type where typtype <> $1 limit 10;".
        // The string is sliced across rows; check for a recognizable mid-query fragment.
        Assert.Contains("select oid, typname, typty", content);
    }

    [Fact]
    public void Convert_Exact_NullTerminatorRenderedAsGlyph()
    {
        var content = RunExact(GetTestPackets(), rowWidthBytes: 32);

        // C-string null terminators must appear as visible \textbackslash 0 glyphs in exact mode.
        Assert.Contains("\\textbackslash 0", content);
    }
}
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace pg_protoexport.tests;

public class PcapToAsciiServiceTests : IDisposable
{
    private readonly string _tempDir;

    public PcapToAsciiServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ascii_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static List<PostgresPacket> ParseExtendedQuery()
    {
        var pcapOptions = new PcapPostgresOptions { RecordFieldMetadata = true };
        pcapOptions.AddDefaultPostgresMessages();
        var pcapService = new PcapService(NullLogger<PcapService>.Instance, Options.Create(pcapOptions));
        return pcapService.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 5432).ToList();
    }

    [Fact]
    public void PcapToAscii_WritesFile_AndIsNonEmpty()
    {
        var packets = ParseExtendedQuery();
        var service = PcapToAsciiService.Create();
        var outputFile = Path.Combine(_tempDir, "out.txt");

        service.PcapToAscii(packets, outputFile);

        Assert.True(File.Exists(outputFile));
        Assert.True(new FileInfo(outputFile).Length > 0);
    }

    [Fact]
    public void Output_ContainsDirectionTaggedHeader_AndNoRulerOrPrefix()
    {
        var packets = ParseExtendedQuery();
        var service = PcapToAsciiService.Create();
        var outputFile = Path.Combine(_tempDir, "out.txt");

        service.PcapToAscii(packets, outputFile);
        var content = File.ReadAllText(outputFile);

        Assert.Matches(@"\[(F->B|B->F)\] \w+ \(\d+ bytes\)", content);
        // No byte-offset column on the left of value lines
        Assert.DoesNotContain("0x0000", content);
        // No "+0  +4  +8" ruler at the top of each message
        Assert.DoesNotMatch(@"\+0\s+\+4\s+\+8", content);
    }

    [Fact]
    public void Output_FieldNames_AreFullyWritten_NotTruncated()
    {
        var packets = ParseExtendedQuery();
        var service = PcapToAsciiService.Create();
        var outputFile = Path.Combine(_tempDir, "out.txt");

        service.PcapToAscii(packets, outputFile);
        var content = File.ReadAllText(outputFile);

        // Long names that the old byte-proportional renderer truncated to "cod" or "para..." now
        // appear in full.
        Assert.Contains("code", content);
        Assert.Contains("statementName", content);
        Assert.Contains("parameterCount", content);
        Assert.Contains("parameterOid[0]", content);
        Assert.Contains("query", content);
    }

    [Fact]
    public void Output_MultiByteFields_HaveByteCountAnnotation()
    {
        var packets = ParseExtendedQuery();
        var service = PcapToAsciiService.Create();
        var outputFile = Path.Combine(_tempDir, "out.txt");

        service.PcapToAscii(packets, outputFile);
        var content = File.ReadAllText(outputFile);

        // The 4-byte length field shows "(4 bytes)" on the value line
        Assert.Contains("(4 bytes)", content);
        // The 2-byte parameterCount shows "(2 bytes)"
        Assert.Contains("(2 bytes)", content);
    }

    [Fact]
    public void Output_OneByteFields_HaveNoByteCountAnnotation()
    {
        // For a 1-byte field with displayed value, the annotation would be redundant — verify
        // the code cell ('Q', 'P', etc.) appears without "(1 byte)" tagged onto it.
        var packets = ParseExtendedQuery();
        var service = PcapToAsciiService.Create();
        var outputFile = Path.Combine(_tempDir, "out.txt");

        service.PcapToAscii(packets, outputFile);
        var content = File.ReadAllText(outputFile);

        Assert.DoesNotMatch(@"'[A-Z]' \(1 byte\)", content);
    }

    [Fact]
    public void Output_LongQueryValue_IsRenderedInFull_NoTruncation()
    {
        var packets = ParseExtendedQuery();
        var service = PcapToAsciiService.Create();
        var outputFile = Path.Combine(_tempDir, "out.txt");

        service.PcapToAscii(packets, outputFile);
        var content = File.ReadAllText(outputFile);

        // The capture's Parse query is: "select oid, typname, typtype from pg_type where ..."
        // Every substring must reach the output — no truncation by "..." inside the rendered value.
        Assert.Contains("select", content);
        Assert.Contains("typname", content);
        Assert.Contains("typtype", content);
        Assert.Contains("pg_type", content);
    }

    [Fact]
    public void Output_MaxWidthOverride_WrapsToMoreRows()
    {
        var packets = ParseExtendedQuery();
        var wideOut = Path.Combine(_tempDir, "wide.txt");
        var narrowOut = Path.Combine(_tempDir, "narrow.txt");

        var serviceWide = PcapToAsciiService.Create(options: new PcapToAsciiOptions { DefaultMaxLineWidth = 400 });
        serviceWide.PcapToAscii(packets, wideOut);

        var serviceNarrow = PcapToAsciiService.Create(options: new PcapToAsciiOptions { DefaultMaxLineWidth = 60 });
        serviceNarrow.PcapToAscii(packets, narrowOut);

        // Narrower max-width → cells wrap to additional rows → more total lines.
        int wideLines = File.ReadAllLines(wideOut).Length;
        int narrowLines = File.ReadAllLines(narrowOut).Length;
        Assert.True(narrowLines > wideLines,
            $"Expected narrow output ({narrowLines} lines) > wide output ({wideLines} lines).");
    }

    [Fact]
    public void Renderer_EmptyParsedFields_FallsBackToSingleBox()
    {
        var sw = new StringWriter();
        AsciiArtRenderer.WriteFields(sw, Array.Empty<ParsedField>(), totalBytes: 17, maxLineWidth: 160);

        var output = sw.ToString();
        Assert.Contains("(17 bytes, unparsed)", output);
    }

    [Fact]
    public void Renderer_DisplayValueWithMultibyteChars_AppearsInOutput()
    {
        var fields = new[]
        {
            new ParsedField("code",   0, 1, "Q"),
            new ParsedField("length", 1, 4, "12"),
            new ParsedField("query",  5, 8, "SELECT 'café'"),
        };

        var sw = new StringWriter();
        AsciiArtRenderer.WriteFields(sw, fields, totalBytes: 13, maxLineWidth: 160);
        var output = sw.ToString();

        Assert.Contains("café", output);
    }

    [Fact]
    public void Renderer_LongValue_IsNotTruncated()
    {
        var longValue = new string('A', 200);
        var fields = new[]
        {
            new ParsedField("code",   0,   1, "Q"),
            new ParsedField("length", 1,   4, "199"),
            new ParsedField("query",  5, 195, longValue),
        };

        var sw = new StringWriter();
        AsciiArtRenderer.WriteFields(sw, fields, totalBytes: 200, maxLineWidth: 400);
        var output = sw.ToString();

        Assert.Contains(longValue, output);
        Assert.Contains("(195 bytes)", output);
    }

    private static List<PostgresPacket> BuildDataRowSequence(int dataRowCount)
    {
        var dataRowDescriptor = new PostgresMessageDescriptor('D', "DataRow", IsFrontEnd: false);
        var commandCompleteDescriptor = new PostgresMessageDescriptor('C', "CommandComplete", IsFrontEnd: false);

        var messages = new List<PostgresMessageBase>();
        for (int i = 0; i < dataRowCount; i++)
            messages.Add(new DataRowMessage(dataRowDescriptor, length: 4) { FieldCount = 0 });
        messages.Add(new CommandCompleteMessage(commandCompleteDescriptor, length: 12) { Message = "SELECT 1" });

        return new List<PostgresPacket>
        {
            new PostgresPacket { Messages = messages },
        };
    }

    [Fact]
    public void Output_ConsecutiveDataRows_AreCollapsed_ByDefault()
    {
        var packets = BuildDataRowSequence(dataRowCount: 5);
        var service = PcapToAsciiService.Create();
        var outputFile = Path.Combine(_tempDir, "collapsed.txt");

        service.PcapToAscii(packets, outputFile);
        var content = File.ReadAllText(outputFile);

        Assert.Single(System.Text.RegularExpressions.Regex.Matches(content, @"\[B->F\] DataRow \(\d+ bytes\)"));
        Assert.Contains("4 DataRow messages skipped", content);
        Assert.Contains("CommandComplete", content);
    }

    [Fact]
    public void Output_MaxDataRowsOverride_RendersEveryDataRow()
    {
        var packets = BuildDataRowSequence(dataRowCount: 5);
        var service = PcapToAsciiService.Create(options: new PcapToAsciiOptions { MaxDataRows = int.MaxValue });
        var outputFile = Path.Combine(_tempDir, "unfiltered.txt");

        service.PcapToAscii(packets, outputFile);
        var content = File.ReadAllText(outputFile);

        Assert.Equal(5, System.Text.RegularExpressions.Regex.Matches(content, @"\[B->F\] DataRow \(\d+ bytes\)").Count);
        Assert.DoesNotContain("DataRow messages skipped", content);
    }

    [Fact]
    public void Output_MaxDataRowsAboveOne_RendersThresholdAndReportsExactSkipCount()
    {
        var packets = BuildDataRowSequence(dataRowCount: 5);
        var service = PcapToAsciiService.Create(options: new PcapToAsciiOptions { MaxDataRows = 2 });
        var outputFile = Path.Combine(_tempDir, "threshold.txt");

        service.PcapToAscii(packets, outputFile);
        var content = File.ReadAllText(outputFile);

        // Renders the first 2 of 5 DataRows and collapses the remaining 3 (not 4 — guards the
        // off-by-one in the skip count when maxDataRows > 1).
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(content, @"\[B->F\] DataRow \(\d+ bytes\)").Count);
        Assert.Contains("3 DataRow messages skipped", content);
        Assert.Contains("CommandComplete", content);
    }

    [Fact]
    public void SequenceDiagram_RendersTwoLifelinesAndArrows()
    {
        var packets = ParseExtendedQuery();
        var service = PcapToAsciiService.Create();
        var outputFile = Path.Combine(_tempDir, "seq.txt");

        service.PcapToSequenceDiagram(packets, outputFile);
        var content = File.ReadAllText(outputFile);

        // Two lifelines (client left, server right) with directional arrows and merged labels.
        Assert.Contains("Client (", content);
        Assert.Contains("Server (", content);
        Assert.Contains("-->", content); // a frontend (C->S) arrow
        Assert.Contains("<--", content); // a backend (S->C) arrow
        Assert.Contains("Parse", content);
    }

    [Fact]
    public void SequenceDiagram_EmptyInput_WritesNoMessagesMarker()
    {
        var sw = new StringWriter();
        AsciiArtRenderer.RenderSequenceDiagram(sw, Array.Empty<PostgresPacket>(), maxLineWidth: 160);

        Assert.Contains("(no messages)", sw.ToString());
    }

    [Fact]
    public void Export_SequenceDiagramMode_ToConsole_WritesToStdout_NotFile()
    {
        var packets = ParseExtendedQuery();
        var service = (IPcapExporter)PcapToAsciiService.Create();
        var outputFile = Path.Combine(_tempDir, "should-not-exist.txt");

        var original = Console.Out;
        var captured = new StringWriter();
        Console.SetOut(captured);
        try
        {
            service.Export(packets, outputFile, PcapToAsciiService.ModeSequenceDiagram,
                new AsciiExportOptions(ToConsole: true));
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = captured.ToString();
        Assert.Contains("Client (", output);
        Assert.Contains("Server (", output);
        Assert.False(File.Exists(outputFile), "Console mode must not create an output file.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(PcapToAsciiService.ModeFields)]
    public void Export_FieldsMode_AndNullMode_ProduceBoxLayout(string? mode)
    {
        var packets = ParseExtendedQuery();
        var service = (IPcapExporter)PcapToAsciiService.Create();
        var outputFile = Path.Combine(_tempDir, $"fields_{mode ?? "null"}.txt");

        service.Export(packets, outputFile, mode, AsciiExportOptions.Default);
        var content = File.ReadAllText(outputFile);

        // Box layout: direction-tagged headers and labelled cells, no lifelines.
        Assert.Matches(@"\[(F->B|B->F)\] \w+ \(\d+ bytes\)", content);
        Assert.Contains("query", content);
        Assert.DoesNotContain("Client (", content);
    }

    private static PostgresPacket FrontEndPacket(params PostgresMessageBase[] msgs) =>
        new() { Messages = msgs.ToList(), IsFrontEnd = true };

    private static PostgresPacket BackEndPacket(params PostgresMessageBase[] msgs) =>
        new() { Messages = msgs.ToList() };

    private static ParseMessage Parse() => new(new('P', "Parse", IsFrontEnd: true), 4);
    private static BindMessage Bind() => new(new('B', "Bind", IsFrontEnd: true), 4);
    private static DescribeMessage Describe() => new(new('D', "Describe", IsFrontEnd: true), 4);
    private static ExecuteMessage Execute() => new(new('E', "Execute", IsFrontEnd: true), 4);
    private static SyncMessage Sync() => new(new('S', "Sync", IsFrontEnd: true), 4);
    private static ParseCompleteMessage ParseComplete() => new(new('1', "ParseComplete", IsFrontEnd: false), 4);
    private static BindCompleteMessage BindComplete() => new(new('2', "BindComplete", IsFrontEnd: false), 4);
    private static RowDescriptionMessage RowDescription() => new(new('T', "RowDescription", IsFrontEnd: false), 4);
    private static DataRowMessage DataRow() => new(new('D', "DataRow", IsFrontEnd: false), 4) { FieldCount = 0 };
    private static CommandCompleteMessage CommandComplete() => new(new('C', "CommandComplete", IsFrontEnd: false), 4);

    [Fact]
    public void SequenceLines_Frontend_SplitsAtExecuteUnlessFollowedBySync()
    {
        // A 3-statement batch: ...Execute / ...Execute / ...Execute / Sync (one packet).
        var packet = FrontEndPacket(
            Parse(), Bind(), Describe(), Execute(),
            Parse(), Bind(), Describe(), Execute(),
            Parse(), Bind(), Describe(), Execute(), Sync());

        var lines = AsciiArtRenderer.BuildAsciiSequenceLines(packet);

        Assert.Equal(3, lines.Count);
        Assert.Equal("Parse / Bind / Describe / Execute", lines[0].Label);
        Assert.Equal("Parse / Bind / Describe / Execute", lines[1].Label);
        // The trailing Execute is immediately followed by Sync, so it stays on the same arrow.
        Assert.Equal("Parse / Bind / Describe / Execute / Sync", lines[2].Label);
        Assert.All(lines, l => Assert.True(l.FrontEnd));
    }

    [Fact]
    public void SequenceLines_Backend_SplitsAtEachCommandComplete()
    {
        var packet = BackEndPacket(
            ParseComplete(), BindComplete(), RowDescription(), DataRow(), CommandComplete(),
            ParseComplete(), BindComplete(), RowDescription(), DataRow(), CommandComplete());

        var lines = AsciiArtRenderer.BuildAsciiSequenceLines(packet);

        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.Equal("ParseComplete / BindComplete / RowDescription / DataRow / CommandComplete", l.Label));
        Assert.All(lines, l => Assert.False(l.FrontEnd));
    }

    [Fact]
    public void SequenceLines_Backend_CollapsesThreeOrMoreDataRows_ButKeepsOneOrTwoIndividual()
    {
        var fourRows = BackEndPacket(RowDescription(), DataRow(), DataRow(), DataRow(), DataRow(), CommandComplete());
        var twoRows = BackEndPacket(RowDescription(), DataRow(), DataRow(), CommandComplete());

        var fourLine = Assert.Single(AsciiArtRenderer.BuildAsciiSequenceLines(fourRows)).Label;
        var twoLine = Assert.Single(AsciiArtRenderer.BuildAsciiSequenceLines(twoRows)).Label;

        Assert.Equal("RowDescription / DataRow (x4) / CommandComplete", fourLine);
        Assert.Equal("RowDescription / DataRow / DataRow / CommandComplete", twoLine);
    }

    [Fact]
    public void CliModule_ExposesTwoBatchVariants_WithDistinctFileNames()
    {
        var variants = new AsciiCliModule().BatchVariants.ToList();

        Assert.Equal(2, variants.Count);
        Assert.All(variants, v => Assert.Equal("ascii", v.Exporter));
        Assert.Contains(variants, v => v.Mode == PcapToAsciiService.ModeFields && v.OutputFileName == "capture.ascii.txt");
        Assert.Contains(variants, v => v.Mode == PcapToAsciiService.ModeSequenceDiagram && v.OutputFileName == "capture.ascii.seq.txt");
        Assert.Equal(variants.Select(v => v.OutputFileName).Distinct().Count(), variants.Count);
    }
}

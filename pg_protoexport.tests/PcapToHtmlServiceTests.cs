using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace pg_protoexport.tests;

public class PcapToHtmlServiceTests : IDisposable
{
    private readonly string _tempDir;

    public PcapToHtmlServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"html_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        // The HTML exporter writes assets (mermaid.min.js etc.) that may still be held briefly
        // by the OS / antivirus on Windows when this Dispose fires under parallel test load.
        // Retry a few times before giving up; failure to clean a temp directory must not break the run.
        TryDeleteDirectory(_tempDir, attempts: 5);
    }

    private static void TryDeleteDirectory(string path, int attempts)
    {
        for (int i = 0; i < attempts; i++)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (i < attempts - 1)
            {
                Thread.Sleep(50 * (i + 1));
            }
            catch (UnauthorizedAccessException) when (i < attempts - 1)
            {
                Thread.Sleep(50 * (i + 1));
            }
            catch (IOException) { return; }
            catch (UnauthorizedAccessException) { return; }
        }
    }

    private string RunHtml()
    {
        var pcapOptions = new PcapPostgresOptions { RecordFieldMetadata = true };
        pcapOptions.AddDefaultPostgresMessages();
        var pcapService = new PcapService(NullLogger<PcapService>.Instance, Options.Create(pcapOptions));
        var packets = pcapService.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 5432);

        var mermaidService = new PcapToMermaidService(NullLogger<PcapToMermaidService>.Instance);
        var htmlService = new PcapToHtmlService(NullLogger<PcapToHtmlService>.Instance, mermaidService);
        var outputFile = Path.Combine(_tempDir, "report.html");
        htmlService.PcapToHtml(packets, outputFile);
        return outputFile;
    }

    [Fact]
    public void PcapToHtml_WritesHtmlFile_WithEmbeddedDataScript()
    {
        // act
        var outputFile = RunHtml();
        var content = File.ReadAllText(outputFile);

        // assert
        Assert.Contains("<script id=\"protoexport-data\"", content);
        Assert.Contains("\"name\":\"Parse\"", content);
        Assert.Contains("sequenceDiagram", content);
    }

    [Fact]
    public void PcapToHtml_CreatesAssetsFolder_WithStylesAppAndMermaid()
    {
        // act
        var outputFile = RunHtml();
        var assetsDir = Path.Combine(_tempDir, "report_assets");

        // assert
        Assert.True(File.Exists(Path.Combine(assetsDir, "styles.css")));
        Assert.True(File.Exists(Path.Combine(assetsDir, "app.js")));
        Assert.True(File.Exists(Path.Combine(assetsDir, "mermaid.min.js")));
    }

    [Fact]
    public void PcapToHtml_DoesNotEmbedHexBytes()
    {
        // act
        var outputFile = RunHtml();
        var content = File.ReadAllText(outputFile);

        // assert — the bytefield view replaces the hex strip; the hex field must not ship in the inline JSON
        Assert.DoesNotContain("\"hex\":\"", content);
        Assert.Contains("\"lengthBytes\":", content);
    }

    [Fact]
    public void PcapToHtml_EmbedsParsedFields_WithOffsetAndLength()
    {
        // act
        var outputFile = RunHtml();
        var content = File.ReadAllText(outputFile);

        // assert — every instrumented message exposes a "length" field at offset 1, 4 bytes
        Assert.Contains("\"name\":\"length\"", content);
        Assert.Contains("\"offset\":1", content);
    }

    [Fact]
    public void PcapToHtml_EmitsAtLeastOneInterlude_ForSamplePcap()
    {
        // act — the sample pcap contains an extended-query batch and a ReadyForQuery
        var outputFile = RunHtml();
        var content = File.ReadAllText(outputFile);

        // assert — the inline JSON carries the interludes array with at least one entry
        Assert.Contains("\"interludes\":[{", content);
        Assert.Contains("\"patternId\":", content);
    }

    [Fact]
    public void Create_ReturnsWorkingService()
    {
        // act
        var service = PcapToHtmlService.Create();

        // assert
        Assert.NotNull(service);
        Assert.IsType<PcapToHtmlService>(service);
    }
}

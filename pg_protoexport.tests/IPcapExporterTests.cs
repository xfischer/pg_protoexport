using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace pg_protoexport.tests;

public class IPcapExporterTests : IDisposable
{
    private readonly string _tempDir;

    public IPcapExporterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"exporter_contract_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        // The HTML exporter writes embedded assets; on Windows these may still be briefly held
        // by the OS / antivirus when Dispose runs under parallel test load. Retry on transient locks.
        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
                return;
            }
            catch (IOException) when (i < 4) { Thread.Sleep(50 * (i + 1)); }
            catch (UnauthorizedAccessException) when (i < 4) { Thread.Sleep(50 * (i + 1)); }
            catch (IOException) { return; }
            catch (UnauthorizedAccessException) { return; }
        }
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddPcapService()
            .AddPcapPortDetector()
            .AddLatexExporter()
            .AddPqTraceExporter()
            .AddMermaidExporter()
            .AddPlantUmlExporter()
            .AddHtmlExporter()
            .AddAsciiExporter();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AllFiveExportersResolveViaIPcapExporter()
    {
        using var sp = BuildProvider();
        var exporters = sp.GetServices<IPcapExporter>().ToList();

        var names = exporters.Select(e => e.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "ascii", "html", "latex", "mermaid", "plantuml", "pqtrace" }, names);
    }

    [Theory]
    [InlineData("latex", ".tex")]
    [InlineData("pqtrace", ".txt")]
    [InlineData("html", ".html")]
    [InlineData("ascii", ".txt")]
    [InlineData("mermaid", ".md")]
    [InlineData("plantuml", ".md")]
    public void Metadata_IsPopulatedCorrectly(string name, string extension)
    {
        using var sp = BuildProvider();
        var exporter = sp.GetServices<IPcapExporter>().Single(e => e.Name == name);

        Assert.Equal(extension, exporter.DefaultExtension);
    }

    [Fact]
    public void CliModules_AggregateExpectedBatchVariants()
    {
        using var sp = BuildProvider();
        var variants = sp.GetServices<IExporterCliModule>()
            .SelectMany(m => m.BatchVariants)
            .Select(v => (v.Exporter, v.OutputFileName))
            .OrderBy(v => v.OutputFileName, StringComparer.Ordinal)
            .ToArray();

        var expected = new[]
        {
            ("ascii", "capture.ascii.txt"),
            ("ascii", "capture.ascii.seq.txt"),
            ("html", "capture.html"),
            ("mermaid", "capture.mermaid.pkt.md"),
            ("mermaid", "capture.mermaid.seq.md"),
            ("plantuml", "capture.plantuml.pkt.md"),
            ("plantuml", "capture.plantuml.seq.md"),
            ("pqtrace", "capture.pqtrace.txt"),
            ("latex", "capture.tex"),
        }.OrderBy(v => v.Item2, StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, variants);
    }

    [Theory]
    [InlineData("latex", null)]
    [InlineData("pqtrace", null)]
    [InlineData("html", null)]
    [InlineData("ascii", null)]
    [InlineData("ascii", "fields")]
    [InlineData("ascii", "sequenceDiagram")]
    [InlineData("mermaid", "sequenceDiagram")]
    [InlineData("mermaid", "packet")]
    [InlineData("plantuml", "sequenceDiagram")]
    [InlineData("plantuml", "packet")]
    public void Export_ProducesNonEmptyOutput(string exporterName, string? mode)
    {
        using var sp = BuildProvider();
        var pcapService = sp.GetRequiredService<IPcapService>();
        var exporter = sp.GetServices<IPcapExporter>().Single(e => e.Name == exporterName);

        var packets = pcapService.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 5432).ToList();
        var outputPath = Path.Combine(_tempDir, $"{exporterName}_{mode ?? "default"}{exporter.DefaultExtension}");

        var result = exporter.Export(packets, outputPath, mode, options: null);

        Assert.NotNull(result);
        Assert.True(File.Exists(outputPath));
        Assert.True(new FileInfo(outputPath).Length > 0);
    }

    [Fact]
    public void Mermaid_Export_WithInvalidMode_Throws()
    {
        using var sp = BuildProvider();
        var exporter = sp.GetServices<IPcapExporter>().Single(e => e.Name == "mermaid");

        Assert.Throws<ArgumentException>(() =>
            exporter.Export(Array.Empty<PostgresPacket>(), Path.Combine(_tempDir, "x.md"), mode: "nope", options: null));
    }

    [Fact]
    public void PlantUml_Export_WithNullMode_Throws()
    {
        using var sp = BuildProvider();
        var exporter = sp.GetServices<IPcapExporter>().Single(e => e.Name == "plantuml");

        Assert.Throws<ArgumentException>(() =>
            exporter.Export(Array.Empty<PostgresPacket>(), Path.Combine(_tempDir, "x.md"), mode: null, options: null));
    }

    [Fact]
    public void Latex_Export_ReturnsRealCounters()
    {
        using var sp = BuildProvider();
        var pcapService = sp.GetRequiredService<IPcapService>();
        var exporter = sp.GetServices<IPcapExporter>().Single(e => e.Name == "latex");

        var packets = pcapService.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 5432).ToList();
        var output = Path.Combine(_tempDir, "out.tex");

        var result = exporter.Export(packets, output, mode: null, options: new LatexExportOptions(Standalone: true));

        Assert.IsType<LatexExportResult>(result);
        Assert.True(result.PacketsProcessed > 0);
        Assert.True(result.MessagesProcessed > 0);
    }

    [Fact]
    public void NonLatex_Export_ReturnsEmptyResult()
    {
        using var sp = BuildProvider();
        var pcapService = sp.GetRequiredService<IPcapService>();
        var exporter = sp.GetServices<IPcapExporter>().Single(e => e.Name == "pqtrace");

        var packets = pcapService.ConvertPcap("TestData/extendedQuery.pcapng", pgsqlPortNumber: 5432).ToList();
        var output = Path.Combine(_tempDir, "out.txt");

        var result = exporter.Export(packets, output, mode: null, options: null);

        Assert.IsType<EmptyExportResult>(result);
    }

    [Fact]
    public void ExportApp_RunExport_RoutesByName()
    {
        using var sp = BuildProvider();
        var pcapService = sp.GetRequiredService<IPcapService>();
        var exporters = sp.GetServices<IPcapExporter>();
        var portDetector = sp.GetRequiredService<IPcapPortDetector>();
        var app = new ExportApp(pcapService, portDetector, exporters, NullLogger<ExportApp>.Instance);

        var output = Path.Combine(_tempDir, "from_app.txt");
        var result = app.RunExport("pqtrace", "TestData/extendedQuery.pcapng", output, port: 5432);

        Assert.NotNull(result);
        Assert.True(File.Exists(output));
        Assert.True(new FileInfo(output).Length > 0);
    }

    [Fact]
    public void ExportApp_RunExport_WithUnknownName_Throws()
    {
        using var sp = BuildProvider();
        var pcapService = sp.GetRequiredService<IPcapService>();
        var exporters = sp.GetServices<IPcapExporter>();
        var portDetector = sp.GetRequiredService<IPcapPortDetector>();
        var app = new ExportApp(pcapService, portDetector, exporters, NullLogger<ExportApp>.Instance);

        Assert.Throws<InvalidOperationException>(() =>
            app.RunExport("does-not-exist", "TestData/extendedQuery.pcapng", Path.Combine(_tempDir, "x"), port: 5432));
    }
}

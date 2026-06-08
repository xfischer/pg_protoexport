using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace pg_protoexport;

/// <summary>
/// Exports captured PostgreSQL messages as labelled ASCII boxes — one box per parsed field,
/// sized to fit its full name and value (with "(N bytes)" annotation on N>1 byte fields).
/// Requires <see cref="PcapPostgresOptions.RecordFieldMetadata"/> to be enabled on the parser
/// (the <c>AddAsciiExporter()</c> DI extension flips this on automatically).
/// </summary>
public class PcapToAsciiService(ILogger<PcapToAsciiService> logger, IOptions<PcapToAsciiOptions> options) : IPcapToAsciiService, IPcapExporter
{
    private readonly ILogger<PcapToAsciiService> _logger = logger;
    private readonly PcapToAsciiOptions _options = options.Value;

    public static IPcapToAsciiService Create(ILoggerFactory? loggerFactory = null, PcapToAsciiOptions? options = null)
    {
        options ??= new PcapToAsciiOptions();
        var log = loggerFactory == null
            ? NullLogger<PcapToAsciiService>.Instance
            : loggerFactory.CreateLogger<PcapToAsciiService>();

        return new PcapToAsciiService(log, Options.Create(options));
    }

    public string Name => "ascii";
    public string DefaultExtension => ".txt";

    public IExportResult Export(IEnumerable<PostgresPacket> packets, string outputPath, string? mode, IExportOptions? options)
    {
        var opts = options as AsciiExportOptions ?? AsciiExportOptions.Default;
        int maxLineWidth = ClampMaxLineWidth(opts.MaxLineWidth ?? _options.DefaultMaxLineWidth);
        int maxDataRows = opts.MaxDataRows ?? _options.MaxDataRows;
        PcapToAscii(packets, outputPath, maxLineWidth, maxDataRows);
        return new EmptyExportResult();
    }

    public void PcapToAscii(IEnumerable<PostgresPacket> packets, string outputFile)
        => PcapToAscii(packets, outputFile, ClampMaxLineWidth(_options.DefaultMaxLineWidth), _options.MaxDataRows);

    public void PcapToAscii(IEnumerable<PostgresPacket> packets, string outputFile, int maxLineWidth)
        => PcapToAscii(packets, outputFile, maxLineWidth, _options.MaxDataRows);

    public void PcapToAscii(IEnumerable<PostgresPacket> packets, string outputFile, int maxLineWidth, int maxDataRows)
    {
        maxLineWidth = ClampMaxLineWidth(maxLineWidth);
        using var writer = new StreamWriter(outputFile, false);

        // Render at most maxDataRows from each run of consecutive DataRow messages, then collapse
        // the remainder into one marker. dataRowRun is the length of the current run; the number
        // actually skipped is (dataRowRun - maxDataRows) once the run ends.
        int dataRowRun = 0;

        foreach (var p in packets)
        {
            foreach (var m in p.Messages)
            {
                if (m is DataRowMessage)
                {
                    if (++dataRowRun > maxDataRows)
                        continue;
                }
                else
                {
                    FlushDataRowSkipMarker(writer, ref dataRowRun, maxDataRows);
                }

                AsciiArtRenderer.RenderMessage(writer, m, maxLineWidth);
                writer.WriteLine();
            }
        }

        FlushDataRowSkipMarker(writer, ref dataRowRun, maxDataRows);
    }

    private static void FlushDataRowSkipMarker(TextWriter writer, ref int dataRowRun, int maxDataRows)
    {
        if (dataRowRun > maxDataRows)
        {
            writer.WriteLine($"[B->F] ... {dataRowRun - maxDataRows} DataRow messages skipped ...");
            writer.WriteLine();
        }
        dataRowRun = 0;
    }

    private static int ClampMaxLineWidth(int n) => Math.Max(40, Math.Min(400, n));
}

using Microsoft.Extensions.Logging;

namespace pg_protoexport;

public class PcapToPqTraceService(ILogger<PcapToPqTraceService> logger) : IPcapToPqTraceService, IPcapExporter
{
    private readonly ILogger<PcapToPqTraceService> _logger = logger;

    public static IPcapToPqTraceService Create(ILoggerFactory? loggerFactory = null)
    {
        return new PcapToPqTraceService(loggerFactory.CreateLoggerOrNull<PcapToPqTraceService>());
    }

    public string Name => "pqtrace";
    public string DefaultExtension => ".txt";

    public IExportResult Export(IEnumerable<PostgresPacket> packets, string outputPath, string? mode, IExportOptions? options)
    {
        PcapToPqTrace(packets, outputPath);
        return new EmptyExportResult();
    }

    public void PcapToPqTrace(IEnumerable<PostgresPacket> packets, string outputFile)
    {
        using var writer = new StreamWriter(outputFile, false);

        foreach (var p in packets)
        {
            writer.WriteLine($"Packet ({(p.IsFrontEnd ? 'F' : 'B')})");

            foreach (var m in p.Messages)
            {
                writer.Write(p.Timestamp.ToString("O"));
                writer.Write('\t');
                writer.Write(m.FrontEnd ? 'F' : 'B');
                writer.Write('\t');
                writer.Write(m.Length);
                writer.Write('\t');
                writer.Write(m.Name);
                writer.Write('\t');
                writer.WriteLine(m.GetStringRepresentation());
            }
        }
    }
}

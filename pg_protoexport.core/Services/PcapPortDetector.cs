using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpPcap;
using SharpPcap.LibPcap;

namespace pg_protoexport;

public sealed class PcapPortDetector(ILogger<PcapPortDetector> logger) : IPcapPortDetector
{
    public static IPcapPortDetector Create(ILoggerFactory? loggerFactory = null) =>
        new PcapPortDetector(loggerFactory?.CreateLogger<PcapPortDetector>() ?? NullLogger<PcapPortDetector>.Instance);

    public ushort Detect(string pcapPath)
    {
        if (string.IsNullOrWhiteSpace(pcapPath))
            throw new ArgumentException("Pcap path is required", nameof(pcapPath));
        if (!File.Exists(pcapPath))
            throw new FileNotFoundException($"Capture file not found: {pcapPath}", pcapPath);

        using var device = new CaptureFileReaderDevice(pcapPath);
        device.Open(new());

        // Two passes worth of state, gathered in a single read:
        //   - SYN-only packets identify a connection's server side authoritatively.
        //   - Per-port packet counts are the fallback when no SYN handshake is captured.
        ushort? synServerPort = null;
        var portCounts = new Dictionary<ushort, int>();
        bool sawAnyTcp = false;

        while (device.GetNextPacket(out PacketCapture e) == GetPacketStatus.PacketRead)
        {
            var rawPacket = e.GetPacket();
            var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
            if (tcpPacket is null) continue;

            sawAnyTcp = true;

            if (synServerPort is null && tcpPacket.Synchronize && !tcpPacket.Acknowledgment)
            {
                // SYN without ACK: client → server. Destination is the listening port.
                synServerPort = (ushort)tcpPacket.DestinationPort;
            }

            ushort src = (ushort)tcpPacket.SourcePort;
            ushort dst = (ushort)tcpPacket.DestinationPort;
            portCounts[src] = portCounts.GetValueOrDefault(src) + 1;
            portCounts[dst] = portCounts.GetValueOrDefault(dst) + 1;
        }

        if (!sawAnyTcp)
            throw new InvalidOperationException($"Cannot infer PostgreSQL port from capture '{pcapPath}': no TCP packets present.");

        if (synServerPort is { } syn)
        {
            logger.LogInformation("Detected server port {Port} from SYN handshake in {Path}", syn, pcapPath);
            return syn;
        }

        // Mid-conversation capture: pick the lowest-numbered port among the most-frequent.
        // Well-known services live in the low range (5432, 5434, 9999, ...); ephemeral
        // client ports are 32768-65535 on Linux, 49152-65535 on Windows.
        var candidates = portCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .ToList();

        var topCount = candidates[0].Value;
        var ties = candidates.TakeWhile(kv => kv.Value == topCount).Select(kv => kv.Key).ToList();
        var pick = ties.Min();

        if (ties.Count > 1)
            logger.LogWarning("Multiple ports tied at {Count} packets in {Path}: {Tied}; picking {Pick}",
                topCount, pcapPath, string.Join(", ", ties), pick);
        else
            logger.LogInformation("Detected server port {Port} from packet-count majority in {Path}", pick, pcapPath);

        return pick;
    }
}

public static class PcapPortDetectorRegistrationExtensions
{
    public static IServiceCollection AddPcapPortDetector(this IServiceCollection services)
    {
        services.AddSingleton<IPcapPortDetector, PcapPortDetector>();
        return services;
    }
}

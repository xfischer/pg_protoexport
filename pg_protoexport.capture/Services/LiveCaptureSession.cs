using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpPcap;
using SharpPcap.LibPcap;

namespace pg_protoexport;

public sealed class LiveCaptureSession : IAsyncDisposable
{
    private const int StartupSettleMs = 150;
    private const int DrainCapMs = 750;

    private readonly LibPcapLiveDevice _device;
    private readonly CaptureFileWriterDevice _writer;
    private readonly ILogger<LiveCaptureSession> _logger;
    private readonly bool _echoPackets;
    private readonly ushort _port;
    private readonly int _readTimeoutMs;
    private readonly object _writeLock = new();
    private long _packetsCaptured;
    private long _bytesCaptured;
    private bool _disposed;

    private LiveCaptureSession(
        LibPcapLiveDevice device,
        CaptureFileWriterDevice writer,
        ILogger<LiveCaptureSession> logger,
        bool echoPackets,
        ushort port,
        int readTimeoutMs)
    {
        _device = device;
        _writer = writer;
        _logger = logger;
        _echoPackets = echoPackets;
        _port = port;
        _readTimeoutMs = readTimeoutMs;
    }

    public string DeviceName => _device.Name;
    public string OutputFile => _writer.Name;
    public long PacketsCaptured => Interlocked.Read(ref _packetsCaptured);
    public long BytesCaptured => Interlocked.Read(ref _bytesCaptured);

    public static async Task<LiveCaptureSession> StartAsync(
        LiveCaptureOptions options,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        var logger = loggerFactory?.CreateLogger<LiveCaptureSession>()
                     ?? NullLogger<LiveCaptureSession>.Instance;

        var device = PcapDevicePicker.Pick(options.Host, options.DeviceName);

        try
        {
            var modes = options.Promiscuous ? DeviceModes.Promiscuous : DeviceModes.None;
            device.Open(modes, options.ReadTimeoutMs);
            device.Filter = options.BpfFilter ?? $"tcp port {options.Port}";
        }
        catch (Exception ex)
        {
            SafeClose(device);
            throw new pg_protoexportException(
                $"Failed to open capture device '{device.Name}': {ex.Message}. " +
                "Capture needs Npcap (Windows) or CAP_NET_RAW / root (Linux/Mac).",
                ex);
        }

        CaptureFileWriterDevice? writer = null;
        try
        {
            writer = new CaptureFileWriterDevice(options.OutputFile, FileMode.Create);
            writer.Open(new DeviceConfiguration { LinkLayerType = device.LinkType });
        }
        catch
        {
            writer?.Close();
            SafeClose(device);
            throw;
        }

        var session = new LiveCaptureSession(device, writer, logger, options.EchoPackets, options.Port, options.ReadTimeoutMs);
        device.OnPacketArrival += session.OnPacketArrival;

        try
        {
            device.StartCapture();
        }
        catch (Exception ex)
        {
            device.OnPacketArrival -= session.OnPacketArrival;
            writer.Close();
            SafeClose(device);
            throw new pg_protoexportException(
                $"Failed to start capture on '{device.Name}': {ex.Message}", ex);
        }

        // SharpPcap's StartCapture() returns before the background capture thread has
        // actually entered pcap_loop. For long-running sessions this is invisible; for
        // short per-scenario captures (a handful of packets, completes in <100ms) the
        // first packets can fly past before the thread is ready and end up unrecorded.
        // Let the thread come up before we let the caller start the workload.
        await Task.Delay(StartupSettleMs, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "live capture started: device='{Device}' filter='{Filter}' -> {Output}",
            device.Name, device.Filter, options.OutputFile);

        return session;
    }

    private void OnPacketArrival(object sender, PacketCapture e)
    {
        try
        {
            var raw = e.GetPacket();
            lock (_writeLock)
            {
                if (_disposed) return;
                _writer.Write(raw);
            }
            Interlocked.Increment(ref _packetsCaptured);
            Interlocked.Add(ref _bytesCaptured, raw.Data?.Length ?? 0);

            if (_echoPackets)
                EchoPacket(raw);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "dropping captured packet: {Message}", ex.Message);
        }
    }

    private void EchoPacket(RawCapture raw)
    {
        var time = raw.Timeval.Date;
        var len = raw.Data?.Length ?? 0;

        try
        {
            var packet = PacketDotNet.Packet.ParsePacket(raw.LinkLayerType, raw.Data);
            var tcp = packet.Extract<PacketDotNet.TcpPacket>();
            if (tcp?.ParentPacket is PacketDotNet.IPPacket ip)
            {
                _logger.LogInformation(
                    "{Hour}:{Min}:{Sec},{Ms} Len={Len} {SrcIp}:{SrcPort} -> {DstIp}:{DstPort}",
                    time.Hour, time.Minute, time.Second, time.Millisecond, len,
                    ip.SourceAddress, tcp.SourcePort, ip.DestinationAddress, tcp.DestinationPort);
                return;
            }
        }
        catch
        {
            // fall through to the bare summary
        }

        _logger.LogInformation(
            "{Hour}:{Min}:{Sec},{Ms} Len={Len}",
            time.Hour, time.Minute, time.Second, time.Millisecond, len);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // libpcap batches packets in a kernel/userspace buffer and only flushes them to
        // OnPacketArrival when the buffer fills OR ReadTimeoutMs expires. For short
        // captures the buffer never fills, so we must wait for at least one timeout
        // worth of time *before* stopping the capture — otherwise pcap_breakloop tears
        // the loop down and any packets still in the buffer are silently dropped.
        // Capped at DrainCapMs so a misconfigured ReadTimeoutMs doesn't stall dispose.
        var drainMs = Math.Min(_readTimeoutMs + 50, DrainCapMs);
        await Task.Delay(drainMs).ConfigureAwait(false);

        try { _device.StopCapture(); }
        catch { /* already stopped */ }

        // Tiny grace period for any callback that's mid-flight to finish writing.
        await Task.Delay(50).ConfigureAwait(false);

        lock (_writeLock)
        {
            if (_disposed) return;
            _disposed = true;
            _device.OnPacketArrival -= OnPacketArrival;
            SafeClose(_device);
            _writer.Close();
        }

        _logger.LogInformation(
            "live capture stopped: {Packets} packets, {Bytes} bytes -> {Output}",
            PacketsCaptured, BytesCaptured, OutputFile);
    }

    private static void SafeClose(LibPcapLiveDevice device)
    {
        try { device.Close(); }
        catch { /* swallow */ }
    }
}

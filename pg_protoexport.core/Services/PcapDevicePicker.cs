using System.Net;
using System.Net.Sockets;
using SharpPcap.LibPcap;

namespace pg_protoexport;

public sealed record PcapDeviceInfo(string Name, string Description, IReadOnlyList<string> Addresses);

public static class PcapDevicePicker
{
    internal static LibPcapLiveDevice Pick(string host, string? explicitDeviceName)
    {
        var devices = LibPcapLiveDeviceList.Instance;
        if (devices.Count == 0)
            throw new pg_protoexportException(
                "No capture devices found. Install Npcap (Windows) or libpcap (Linux/Mac) and ensure the process has the required privileges.");

        if (!string.IsNullOrWhiteSpace(explicitDeviceName))
        {
            var byName = devices.FirstOrDefault(d =>
                string.Equals(d.Name, explicitDeviceName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.Description, explicitDeviceName, StringComparison.OrdinalIgnoreCase));
            if (byName is null)
                throw new pg_protoexportException(
                    $"Capture device '{explicitDeviceName}' not found. Available: {DeviceList(devices)}");
            return byName;
        }

        IPAddress[] hostAddrs;
        try { hostAddrs = Dns.GetHostAddresses(host); }
        catch (Exception ex)
        {
            throw new pg_protoexportException($"Could not resolve host '{host}': {ex.Message}", ex);
        }

        if (hostAddrs.Any(IPAddress.IsLoopback))
        {
            var loopback = devices.FirstOrDefault(IsLoopback);
            if (loopback is not null) return loopback;
            throw new pg_protoexportException(
                "Host resolves to loopback but no loopback adapter is exposed by libpcap/Npcap. " +
                "On Windows, install Npcap with the 'WinPcap API-compatible Mode' option enabled. " +
                $"Available: {DeviceList(devices)}");
        }

        var localIp = TryFindLocalIpForHost(hostAddrs);
        if (localIp is null)
            throw new pg_protoexportException(
                $"Could not determine which local interface routes to '{host}'. " +
                $"Pass --device explicitly. Available: {DeviceList(devices)}");

        var match = devices.FirstOrDefault(d => HasAddress(d, localIp));
        if (match is null)
            throw new pg_protoexportException(
                $"No capture device with local IP {localIp} found for host '{host}'. " +
                $"Pass --device explicitly. Available: {DeviceList(devices)}");
        return match;
    }

    public static IReadOnlyList<PcapDeviceInfo> Enumerate()
    {
        return LibPcapLiveDeviceList.Instance
            .Select(d => new PcapDeviceInfo(
                d.Name,
                d.Description ?? "",
                d.Addresses
                    .Select(a => a?.Addr?.ipAddress)
                    .Where(ip => ip is not null)
                    .Select(ip => ip!.ToString())
                    .ToList()))
            .ToList();
    }

    private static bool IsLoopback(LibPcapLiveDevice device)
    {
        if (device.Name is "lo" or "lo0") return true;
        if (device.Name?.Contains("loopback", StringComparison.OrdinalIgnoreCase) == true) return true;
        if (device.Description?.Contains("loopback", StringComparison.OrdinalIgnoreCase) == true) return true;
        return device.Addresses.Any(a => a?.Addr?.ipAddress is { } ip && IPAddress.IsLoopback(ip));
    }

    private static bool HasAddress(LibPcapLiveDevice device, IPAddress target)
    {
        return device.Addresses.Any(a => target.Equals(a?.Addr?.ipAddress));
    }

    private static IPAddress? TryFindLocalIpForHost(IPAddress[] hostAddrs)
    {
        foreach (var addr in hostAddrs)
        {
            try
            {
                using var sock = new Socket(addr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                sock.Connect(addr, 65530);
                if (sock.LocalEndPoint is IPEndPoint ep)
                    return ep.Address;
            }
            catch
            {
                // try the next address family
            }
        }
        return null;
    }

    private static string DeviceList(LibPcapLiveDeviceList devices)
        => string.Join(", ", devices.Select(d => d.Name));
}

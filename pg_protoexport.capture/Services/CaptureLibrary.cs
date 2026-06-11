using System.Runtime.InteropServices;
using SharpPcap.LibPcap;

namespace pg_protoexport;

/// <summary>
/// Centralizes access to SharpPcap's native-backed device list so that a missing
/// native capture library (Npcap / libpcap) is detected once and reported with a
/// platform-specific download link instead of a raw <see cref="DllNotFoundException"/>.
/// </summary>
internal static class CaptureLibrary
{
    /// <summary>
    /// Returns the live device list, translating a missing native library into a
    /// <see cref="CaptureLibraryMissingException"/>. The first native call SharpPcap
    /// makes (enumerating devices) throws <see cref="DllNotFoundException"/> when the
    /// library is absent; the type-initializer path may wrap it in a
    /// <see cref="TypeInitializationException"/>.
    /// </summary>
    internal static LibPcapLiveDeviceList GetLiveDevices()
    {
        try
        {
            return LibPcapLiveDeviceList.Instance;
        }
        catch (Exception ex) when (IsMissingNativeLibrary(ex))
        {
            throw new CaptureLibraryMissingException(BuildMessage(), DownloadUrl, ex);
        }
    }

    private static bool IsMissingNativeLibrary(Exception ex) =>
        ex is DllNotFoundException ||
        (ex is TypeInitializationException tie && tie.InnerException is DllNotFoundException);

    /// <summary>The platform-appropriate place to obtain the capture library.</summary>
    internal static string DownloadUrl
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "https://npcap.com/#download";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "https://formulae.brew.sh/formula/libpcap";
            return "https://www.tcpdump.org/";
        }
    }

    /// <summary>A human-readable, platform-specific explanation and fix.</summary>
    internal static string BuildMessage()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Npcap is not installed. pg_protoexport needs Npcap to capture live traffic. "
                 + "Install it, enabling \"WinPcap API-compatible Mode\" so loopback captures work.";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "libpcap is not available. macOS normally ships with it; if it is missing, "
                 + "install it with: brew install libpcap. "
                 + "Capture also needs root or the CAP_NET_RAW capability.";

        return "libpcap is not installed. Install it with your package manager "
             + "(e.g. apt install libpcap0.8, or dnf install libpcap). "
             + "Capture also needs root or the CAP_NET_RAW capability.";
    }
}

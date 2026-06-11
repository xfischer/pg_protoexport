namespace pg_protoexport;

/// <summary>
/// Thrown when the native packet-capture library (Npcap on Windows, libpcap on
/// Linux/macOS) that SharpPcap depends on is not installed. Carries a
/// platform-specific <see cref="DownloadUrl"/> so callers can point the user at
/// the right download. Derives from <see cref="pg_protoexportException"/> so
/// existing catch sites keep working.
/// </summary>
public sealed class CaptureLibraryMissingException : pg_protoexportException
{
    public CaptureLibraryMissingException(string message, string downloadUrl, Exception inner)
        : base(message, inner)
    {
        DownloadUrl = downloadUrl;
    }

    /// <summary>Where the user can obtain the missing capture library.</summary>
    public string DownloadUrl { get; }
}

namespace pg_protoexport;

/// <summary>
/// Sniffs a .pcap/.pcapng file's TCP headers and picks the most-likely PostgreSQL server port.
/// Lets CLI commands accept captures without the user having to remember which port the
/// server was running on.
/// </summary>
public interface IPcapPortDetector
{
    /// <summary>
    /// Returns the inferred server-side TCP port for the given capture file.
    /// Throws <see cref="InvalidOperationException"/> when the capture contains no TCP traffic.
    /// </summary>
    ushort Detect(string pcapPath);
}

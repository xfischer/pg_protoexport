namespace pg_protoexport;

/// <summary>
/// Format-agnostic "time since last packet" computation shared by the LaTeX, ASCII, and HTML
/// exporters. Each caller wraps the plain text returned here in its own markup.
/// </summary>
public static class PacketTimeline
{
    public static string FormatAbsolute(DateTime timestamp) => timestamp.ToString("HH:mm:ss.ffffff");

    /// <summary>
    /// Elapsed time in whole microseconds. Capture timestamps (libpcap/SharpPcap <c>timeval</c>)
    /// are natively microsecond-resolution, so this loses no real precision (unlike millisecond
    /// truncation, which collapsed most intra-conversation deltas on loopback captures to "+0 ms").
    /// </summary>
    public static long DeltaMicroseconds(TimeSpan delta) => (long)delta.TotalMicroseconds;

    /// <summary>Plain-text rendering for ASCII/HTML output. LaTeX callers should not use this
    /// directly — the micro sign is not safe to embed as a raw glyph in LaTeX source; use
    /// <see cref="DeltaMicroseconds"/> with a math-mode <c>$\mu$s</c> unit instead.</summary>
    public static string FormatDelta(TimeSpan delta) => $"+{DeltaMicroseconds(delta)} µs";
}

/// <summary>
/// Stateful per-export cursor that turns a stream of packet timestamps into timeline annotations.
/// The first call anchors the timeline and returns no delta; every later call returns both the
/// elapsed time since the immediately preceding call (<c>Delta</c>) and since the very first call
/// (<c>Total</c>), so a reader can see local latency and overall progress through the capture at
/// the same time without mentally summing deltas.
/// </summary>
public sealed class PacketTimelineTracker
{
    private DateTime? _first;
    private DateTime? _last;

    public (bool IsFirst, string AbsoluteText, TimeSpan? Delta, TimeSpan? Total) Advance(DateTime current)
    {
        bool isFirst = _first is null;
        string absolute = PacketTimeline.FormatAbsolute(current);
        TimeSpan? delta = isFirst ? null : current - _last!.Value;
        TimeSpan? total = isFirst ? null : current - _first!.Value;
        _first ??= current;
        _last = current;
        return (isFirst, absolute, delta, total);
    }
}

namespace pg_protoexport;

/// <summary>
/// DI-bound defaults for the ASCII exporter. CLI flags override these per-call via
/// <see cref="AsciiExportOptions"/>.
/// </summary>
public sealed class PcapToAsciiOptions
{
    /// <summary>
    /// Maximum number of characters per output line before the renderer wraps cells onto a new row.
    /// Clamped to [40, 400] at CLI parse time. Default 160 fits comfortably in modern terminals
    /// while leaving room for long query strings on a single row.
    /// </summary>
    public int DefaultMaxLineWidth { get; set; } = 160;

    /// <summary>
    /// Maximum number of consecutive DataRow messages to render before collapsing the rest of the
    /// run into a single "... N DataRow messages skipped ..." line. Mirrors
    /// <c>PcapToLatexOptions.MaxDataRows</c>. Set to a very large value to disable collapsing.
    /// </summary>
    public int MaxDataRows { get; set; } = 1;
}

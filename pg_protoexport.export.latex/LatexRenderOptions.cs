namespace pg_protoexport;

/// <summary>
/// Per-export render configuration for the LaTeX exporter.
/// Carries the user's choice between the default "nice" rendering and the byte-exact rendering,
/// plus the bytefield row width used for wrapping in exact mode.
/// </summary>
public sealed record LatexRenderOptions
{
    /// <summary>
    /// When true, every <c>\bitbox{N}</c> is laid out with <c>N</c> equal to the actual on-the-wire byte
    /// count of the field it represents; long content wraps to successive rows and string content is
    /// rendered as LaTeX-escaped literal UTF-8 (no truncation).
    /// </summary>
    public bool Exact { get; init; } = false;

    /// <summary>
    /// Bytefield row width in bytes. Used both as the <c>\begin{bytefield}{N}</c> declaration and as the
    /// wrapping boundary in exact mode. Typical values: 16, 32, 64. Clamped to [8, 256] at CLI parse time.
    /// </summary>
    public int RowWidthBytes { get; init; } = 32;

    /// <summary>Singleton default (nice mode, 32-byte rows) used when no options are supplied.</summary>
    public static LatexRenderOptions Default { get; } = new();
}

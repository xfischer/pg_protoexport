namespace pg_protoexport;

/// <summary>
/// Options for the LaTeX exporter passed via <see cref="IPcapExporter.Export"/>.
/// Mirrors the arguments of the typed <c>PcapToLaTeX</c> / <c>PcapToLaTeX_MultipleFiles</c>
/// methods so the unified contract can drive the same logic.
/// </summary>
public sealed record LatexExportOptions(
    bool Standalone = true,
    bool MultipleFiles = false,
    LatexRenderOptions? Render = null) : IExportOptions
{
    public static readonly LatexExportOptions Default = new();
}

/// <summary>
/// LaTeX-specific export result that projects the counters from <see cref="GenerationState"/>.
/// </summary>
public sealed record LatexExportResult(
    int PacketsProcessed,
    int MessagesProcessed,
    int MessagesInvalid) : IExportResult
{
    internal static LatexExportResult From(GenerationState state) => new(
        state.StatsPacketsProcessed,
        state.StatsMessagesProcessed,
        state.StatsMessagesInvalid);
}

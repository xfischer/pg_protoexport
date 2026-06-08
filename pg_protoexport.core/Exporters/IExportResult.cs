namespace pg_protoexport;

/// <summary>
/// Counters reported by <see cref="IPcapExporter.Export"/>. Exporters that do not track
/// per-message stats may return <see cref="EmptyExportResult"/>.
/// </summary>
public interface IExportResult
{
    int PacketsProcessed { get; }
    int MessagesProcessed { get; }
    int MessagesInvalid { get; }
}

/// <summary>
/// Zero-count result. Suitable for exporters that do not track stats.
/// </summary>
public sealed record EmptyExportResult(
    int PacketsProcessed = 0,
    int MessagesProcessed = 0,
    int MessagesInvalid = 0) : IExportResult;

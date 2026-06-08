namespace pg_protoexport;

/// <summary>
/// Host-side coordinator that resolves an exporter by name and runs it. Exporter CLI commands
/// depend on this abstraction rather than the concrete <see cref="ExportApp"/>.
/// </summary>
public interface IExportApp
{
    /// <summary>Parses <paramref name="inputFile"/> (auto-detecting the port when <paramref name="port"/> is null) and runs the named exporter.</summary>
    IExportResult RunExport(
        string exporterName,
        string inputFile,
        string outputPath,
        ushort? port,
        string? mode = null,
        IExportOptions? options = null);

    /// <summary>Runs the named exporter against an already-parsed packet list (used to fan one capture out to many variants).</summary>
    IExportResult RunExportPrebuilt(
        IReadOnlyList<PostgresPacket> packets,
        string exporterName,
        string outputPath,
        string? mode = null,
        IExportOptions? options = null);
}

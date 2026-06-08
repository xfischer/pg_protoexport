using Spectre.Console.Cli;

namespace pg_protoexport;

/// <summary>
/// Contributed by each exporter project so the CLI host can register that exporter's command(s)
/// and batch variants generically. One implementation is registered into DI per exporter (by the
/// exporter's <c>Add{Format}Exporter()</c> extension) and discovered by the host at startup, so
/// adding a new exporter requires no edits to the <c>pg_protoexport</c> CLI project.
/// </summary>
public interface IExporterCliModule
{
    /// <summary>Registers this exporter's command (or branch of sub-commands), including examples.</summary>
    void Register(IConfigurator config);

    /// <summary>The outputs this exporter contributes to the <c>batchexport</c> command.</summary>
    IEnumerable<BatchVariant> BatchVariants { get; }
}

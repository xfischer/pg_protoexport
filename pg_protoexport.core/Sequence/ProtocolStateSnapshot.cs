using System.Collections.Immutable;

namespace pg_protoexport;

public enum ConnectionState
{
    Initial,
    StartupSent,
    Authenticating,
    Authenticated,
    Ready,
    Terminated
}

/// <summary>
/// Immutable snapshot of observable protocol state after a given message.
/// Produced per-message by <see cref="ProtocolStateProjector"/>.
/// </summary>
public sealed record ProtocolStateSnapshot(
    ConnectionState ConnState,
    TransactionStatus TxStatus,
    ImmutableDictionary<string, string> Prepared,
    ImmutableDictionary<string, string> Portals,
    ImmutableDictionary<string, string> ServerParams,
    int? BackendPid,
    CopyStreamKind CopyMode,
    CopyStreamFormat? CopyFormat)
{
    public static readonly ProtocolStateSnapshot Empty = new(
        ConnState: ConnectionState.Initial,
        TxStatus: TransactionStatus.Unknown,
        Prepared: ImmutableDictionary<string, string>.Empty,
        Portals: ImmutableDictionary<string, string>.Empty,
        ServerParams: ImmutableDictionary<string, string>.Empty,
        BackendPid: null,
        CopyMode: CopyStreamKind.None,
        CopyFormat: null);
}

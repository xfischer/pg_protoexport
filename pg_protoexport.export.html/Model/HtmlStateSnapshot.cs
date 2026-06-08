namespace pg_protoexport;

public sealed record HtmlStateSnapshot(
    string ConnState,
    string TxStatus,
    Dictionary<string, string> Prepared,
    Dictionary<string, string> Portals,
    Dictionary<string, string> ServerParams,
    int? BackendPid,
    string? CopyMode,
    string? CopyFormat);

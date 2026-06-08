namespace pg_protoexport;

/// <summary>
/// Kind of startup-phase probe the client most recently sent on a given port,
/// used to interpret the server's single-byte reply ('S'/'N' after SSL, 'G'/'N' after GSSENC).
/// </summary>
public enum StartupProbeKind { None, SSL, GSSENC }

/// <summary>
/// Which COPY sub-protocol a connection is currently inside, if any. <see cref="None"/> means no
/// COPY stream is active. <see cref="In"/> = COPY ... FROM STDIN (client streams data),
/// <see cref="Out"/> = COPY ... TO STDOUT (server streams data), <see cref="Both"/> = replication.
/// </summary>
public enum CopyStreamKind { None, In, Out, Both }

/// <summary>
/// Whether an active COPY stream carries the text or binary on-the-wire format
/// (PostgreSQL 18 <c>COPY ... WITH (FORMAT BINARY)</c> vs. the default text format).
/// </summary>
public enum CopyStreamFormat { Text, Binary }

/// <summary>
/// Per-connection state for an in-flight COPY exchange. <see cref="ChunkCount"/> counts the
/// CopyData frames seen so far on this stream — used to identify the FIRST chunk (where the
/// binary file header lives, if <see cref="Format"/> = Binary).
/// </summary>
public sealed record CopyStreamState(CopyStreamKind Kind, CopyStreamFormat Format, int ChunkCount);

/// <summary>
/// State of the Pcap reader, with properties to track the current state of the reader
/// </summary>
public class PcapReadState
{
    private readonly Dictionary<int, StartupProbeKind> _lastStartupProbe = [];
    private readonly Dictionary<int, AuthenticationGenericMessage?> _lastAuthPacket = [];
    private readonly Dictionary<(int Pid, uint Secret), ushort> _cancelKeyToClientPort = [];
    private readonly Dictionary<ushort, CopyStreamState> _copyStreams = [];

    /// <summary>
    /// Current TCP Port being read
    /// </summary>
    public ushort Port { get; internal set; }

    /// <summary>
    /// Buffer leftover from previous packet, needed for packet reconstruction
    /// </summary>
    public byte[]? PreviousBufferLeftover { get; internal set; } = null;

    /// <summary>
    /// Last Row description, useful when processing DataRow messages
    /// </summary>
    public RowDescriptionMessage? LastRowDescription { get; internal set; }

    internal StartupProbeKind LastStartupProbe(int port) =>
        _lastStartupProbe.TryGetValue(port, out var kind) ? kind : StartupProbeKind.None;

    internal void SetLastStartupProbe(int port, StartupProbeKind kind)
    {
        _lastStartupProbe[port] = kind;
    }

    internal AuthenticationGenericMessage? GetLastAuthPacket(int port)
    {
        if (_lastAuthPacket.TryGetValue(port, out var message))
        {
            _lastAuthPacket.Remove(port);
            return message;
        }
        return null;
    }

    internal void SetLastAuthPacket(int port, AuthenticationGenericMessage? message)
    {
        _lastAuthPacket[port] = message;
    }

    /// <summary>
    /// Records that a server-issued (pid, secret) belongs to the conversation owning the given client port.
    /// Looked up later when a CancelRequest carrying the same (pid, secret) arrives on a different connection.
    /// </summary>
    internal void RegisterCancelKey(int processId, uint secretKey, ushort clientPort)
    {
        _cancelKeyToClientPort[(processId, secretKey)] = clientPort;
    }

    /// <summary>
    /// Returns the client port of the original conversation that received (pid, secret) in
    /// BackendKeyData, or null if no match has been seen yet (the cancel may target a session
    /// not present in this capture).
    /// </summary>
    internal ushort? LookupCancelTargetClientPort(int processId, uint secretKey) =>
        _cancelKeyToClientPort.TryGetValue((processId, secretKey), out var port) ? port : null;

    /// <summary>
    /// Marks the given client port as being inside a COPY stream of the given kind+format.
    /// Resets <see cref="CopyStreamState.ChunkCount"/> to 0 so the next CopyData is recognised
    /// as the first chunk. Called when a CopyInResponse / CopyOutResponse / CopyBothResponse is parsed.
    /// </summary>
    internal void EnterCopyStream(ushort clientPort, CopyStreamKind kind, CopyStreamFormat format)
    {
        _copyStreams[clientPort] = new CopyStreamState(kind, format, 0);
    }

    /// <summary>
    /// Records that a new CopyData chunk is about to be parsed on the given port and returns the
    /// PRE-increment state — so the caller can tell "this is chunk N" where N = state.ChunkCount.
    /// Returns null when no COPY stream is active on this port (orphan CopyData / capture started
    /// mid-stream).
    /// </summary>
    internal CopyStreamState? BeginCopyDataChunk(ushort clientPort)
    {
        if (!_copyStreams.TryGetValue(clientPort, out var s))
            return null;
        _copyStreams[clientPort] = s with { ChunkCount = s.ChunkCount + 1 };
        return s;
    }

    /// <summary>Clears any active COPY stream on the given client port (CopyDone / CopyFail / ErrorResponse).</summary>
    internal void ExitCopyStream(ushort clientPort)
    {
        _copyStreams.Remove(clientPort);
    }

    /// <summary>Read-only access to the current COPY stream state, or null if none is active.</summary>
    internal CopyStreamState? GetCopyStream(ushort clientPort) =>
        _copyStreams.TryGetValue(clientPort, out var s) ? s : null;
}

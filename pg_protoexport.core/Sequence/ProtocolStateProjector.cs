namespace pg_protoexport;

/// <summary>
/// Folds over a packet stream and yields a <see cref="ProtocolStateSnapshot"/> after every message.
/// Observable transitions only — no inferred state.
/// </summary>
public static class ProtocolStateProjector
{
    public static IEnumerable<(PostgresPacket Packet, PostgresMessageBase Message, ProtocolStateSnapshot Snapshot)>
        Project(IEnumerable<PostgresPacket> packets)
    {
        var state = ProtocolStateSnapshot.Empty;
        foreach (var packet in packets)
        {
            foreach (var message in packet.Messages)
            {
                state = Advance(state, message);
                yield return (packet, message, state);
            }
        }
    }

    private static ProtocolStateSnapshot Advance(ProtocolStateSnapshot s, PostgresMessageBase message)
    {
        switch (message)
        {
            case StartupMessageMessage:
                return s with { ConnState = ConnectionState.StartupSent };

            case AuthenticationGenericMessage auth:
                return auth.AuthenticationName == "AuthenticationOK"
                    ? s with { ConnState = ConnectionState.Authenticating }
                    : s with { ConnState = ConnectionState.Authenticating };

            case ReadyForQueryMessage rfq:
                return s with
                {
                    ConnState = ConnectionState.Ready,
                    TxStatus = rfq.Status,
                    Portals = s.Portals.IsEmpty ? s.Portals : s.Portals.Clear()
                };

            case TerminateMessage:
                return s with { ConnState = ConnectionState.Terminated };

            case ParseMessage parse:
                return s with { Prepared = s.Prepared.SetItem(parse.Statement, parse.Query) };

            case BindMessage bind:
                return s with { Portals = s.Portals.SetItem(bind.PortalName, bind.StatementName) };

            case ParameterStatusMessage ps:
                return s with { ServerParams = s.ServerParams.SetItem(ps.ParameterName, ps.Value) };

            case BackendKeyDataMessage bkd:
                return s with { BackendPid = bkd.ProcessId };

            case CopyInResponseMessage cir:
                return s with
                {
                    CopyMode = CopyStreamKind.In,
                    CopyFormat = cir.OverallFormat == 1 ? CopyStreamFormat.Binary : CopyStreamFormat.Text
                };

            case CopyOutResponseMessage cor:
                return s with
                {
                    CopyMode = CopyStreamKind.Out,
                    CopyFormat = cor.OverallFormat == 1 ? CopyStreamFormat.Binary : CopyStreamFormat.Text
                };

            case CopyBothResponseMessage cbr:
                return s with
                {
                    CopyMode = CopyStreamKind.Both,
                    CopyFormat = cbr.OverallFormat == 1 ? CopyStreamFormat.Binary : CopyStreamFormat.Text
                };

            case CopyDoneMessage:
            case CopyFailMessage:
                return s with { CopyMode = CopyStreamKind.None, CopyFormat = null };

            case ErrorResponseMessage when s.CopyMode != CopyStreamKind.None:
                // A server error during COPY aborts the stream — clear the mode.
                return s with { CopyMode = CopyStreamKind.None, CopyFormat = null };

            default:
                return s;
        }
    }
}

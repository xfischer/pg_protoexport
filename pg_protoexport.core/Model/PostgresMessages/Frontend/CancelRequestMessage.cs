using System.Diagnostics;

namespace pg_protoexport;

public class CancelRequestMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public const int MagicCode = 80877102;

    public int RequestCode { get; init; }
    public int ProcessId { get; init; }
    public int SecretKey { get; init; }

    /// <summary>
    /// Client port of the original conversation whose BackendKeyData carried this (pid, secret),
    /// or null if no matching session appears earlier in the capture (e.g. the targeted session
    /// happened in a separate capture).
    /// </summary>
    public ushort? CorrelatedClientPort { get; internal set; }

    internal static CancelRequestMessage Read(PostgresMessageDescriptor pgMessage, int messageLength, int requestCode, PcapBinaryReader reader)
    {
        Debug.Assert(requestCode == MagicCode);
        int pid, secret;
        using (reader.BeginField("processId")) pid = reader.ReadInt32();
        using (reader.BeginField("secretKey")) secret = reader.ReadInt32();
        return new CancelRequestMessage(pgMessage, messageLength)
        {
            RequestCode = requestCode,
            ProcessId = pid,
            SecretKey = secret
        };
    }

    public override string GetStringRepresentation() => $"pid={ProcessId}, secret={SecretKey:X8}";
}

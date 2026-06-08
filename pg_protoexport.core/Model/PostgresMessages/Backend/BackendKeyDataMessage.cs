using System.Diagnostics;

namespace pg_protoexport;

public class BackendKeyDataMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public int ProcessId { get; init; }
    public uint SecretKey { get; init; }

    internal static BackendKeyDataMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        Debug.Assert(len == 12);
        int pid;
        uint secret;
        using (reader.BeginField("processId")) pid = reader.ReadInt32();
        using (reader.BeginField("secretKey")) secret = reader.ReadUInt32();
        return new BackendKeyDataMessage(pgMessage, len)
        {
            ProcessId = pid,
            SecretKey = secret
        };
    }
    public override string GetStringRepresentation() => $"PID: {ProcessId}, SecretKey: {SecretKey}";
}
namespace pg_protoexport;

/// <summary>
/// Sender signals end-of-stream for a COPY exchange. Header-only (length must be 4).
/// Bidirectional: emitted by either side of a COPY.
/// </summary>
public class CopyDoneMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    internal static CopyDoneMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        return new CopyDoneMessage(pgMessage, len);
    }
}

namespace pg_protoexport;

public class TerminateMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    internal static TerminateMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        var packet = new TerminateMessage(pgMessage, len);

        return packet;
    }
}
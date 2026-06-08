namespace pg_protoexport;

public class BindCompleteMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    internal static BindCompleteMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        return new BindCompleteMessage(pgMessage, len);
    }

    public override string GetStringRepresentation() => string.Empty;
}
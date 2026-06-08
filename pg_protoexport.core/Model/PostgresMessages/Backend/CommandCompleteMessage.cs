namespace pg_protoexport;

public class CommandCompleteMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public string Message { get; init; } = "";

    internal static CommandCompleteMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        string msg;
        using (reader.BeginField("message")) msg = reader.ReadNullTerminatedString(len);
        return new CommandCompleteMessage(pgMessage, len)
        {
            Message = msg
        };
    }

    public override string GetStringRepresentation() => Message;
}
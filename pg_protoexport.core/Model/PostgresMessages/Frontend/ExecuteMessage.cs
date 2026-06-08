namespace pg_protoexport;

public class ExecuteMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public string PortalName { get; internal set; } = "";
    public int MaxRows { get; internal set; }

    internal static ExecuteMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        var message = new ExecuteMessage(pgMessage, len);
        using (reader.BeginField("portalName")) message.PortalName = reader.ReadNullTerminatedString(len);
        using (reader.BeginField("maxRows")) message.MaxRows = reader.ReadInt32();
        return message;
    }

    public override string GetStringRepresentation() => $"portal='{PortalName}', maxrows={MaxRows}";
}
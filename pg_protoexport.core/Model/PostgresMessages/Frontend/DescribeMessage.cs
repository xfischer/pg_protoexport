namespace pg_protoexport;

public class DescribeMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public char PortalOrStatement { get; internal set; }
    public string PortalOrStatementName { get; internal set; } = "";

    internal static DescribeMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        var message = new DescribeMessage(pgMessage, len);
        using (reader.BeginField("portalOrStatement")) message.PortalOrStatement = reader.ReadChar();
        using (reader.BeginField("portalOrStatementName")) message.PortalOrStatementName = reader.ReadNullTerminatedString(len);
        return message;
    }

    public override string GetStringRepresentation() => $"{(PortalOrStatement == 'P' ? "Portal" : "Statement")}: " +
        $"'{PortalOrStatementName}'";
}
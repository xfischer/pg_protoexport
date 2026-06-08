namespace pg_protoexport;

public class SASLInitialResponseMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public string Mechanism { get; init; } = "";
    public int InitialResponseLength { get; init; }
    public byte[] InitialResponse { get; private set; } = [];

    internal static SASLInitialResponseMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        string mechanism;
        using (reader.BeginField("mechanism")) mechanism = reader.ReadNullTerminatedString(len);
        int initialResponseLength;
        using (reader.BeginField("initialResponseLength")) initialResponseLength = reader.ReadInt32();
        var packet = new SASLInitialResponseMessage(pgMessage, len)
        {
            Mechanism = mechanism,
            InitialResponseLength = initialResponseLength
        };
        if (packet.InitialResponseLength > 0)
        {
            using (reader.BeginField("initialResponse")) packet.InitialResponse = reader.ReadBytes(packet.InitialResponseLength);
        }

        return packet;
    }
}

namespace pg_protoexport;

public class AuthenticationSASLMessage(PostgresMessageDescriptor pgMessage, int length, int intData) : AuthenticationGenericMessage(pgMessage, length, intData, "AuthenticationSASL")
{
    public List<string> Mechanisms { get; } = [];

    internal static AuthenticationSASLMessage Read(PostgresMessageDescriptor pgMessage, int length, int intData, PcapBinaryReader reader)
    {
        var packet = new AuthenticationSASLMessage(pgMessage, length, intData);

        string mechanism;
        int index = 0;
        do
        {
            using (reader.BeginField($"mechanism[{index}]")) mechanism = reader.ReadNullTerminatedString(length);
            if (!string.IsNullOrEmpty(mechanism))
            {
                packet.Mechanisms.Add(mechanism);
                index++;
            }
        }
        while (mechanism != "");

        return packet;
    }

    internal override PostgresMessageBase ReadResponseMessage(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader) => SASLInitialResponseMessage.Read(pgMessage, reader);
}

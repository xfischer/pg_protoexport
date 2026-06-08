namespace pg_protoexport;

public class AuthenticationSASLContinueMessage(PostgresMessageDescriptor pgMessage, int len, int intData, byte[] bytes) : AuthenticationGenericMessage(pgMessage, len, intData, "AuthenticationSASLContinue")
{
    public byte[] SASLData { get; } = bytes;

    internal override PostgresMessageBase ReadResponseMessage(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader) => SASLResponseMessage.Read(pgMessage, reader);
}

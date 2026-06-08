namespace pg_protoexport;

public class AuthenticationSASLFinalMessage(PostgresMessageDescriptor pgMessage, int len, int intData, byte[] bytes) : AuthenticationGenericMessage(pgMessage, len, intData, "AuthenticationSASLFinal")
{
    public byte[] SASLOutcome { get; } = bytes;

    internal override PostgresMessageBase ReadResponseMessage(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader) => throw new NotImplementedException();
}

namespace pg_protoexport;

public class AuthenticationGSSContinueMessage(PostgresMessageDescriptor pgMessage, int len, int intData, byte[] bytes) : AuthenticationGenericMessage(pgMessage, len, intData, "AuthenticationGSSContinue")
{
    public byte[] AuthData { get; } = bytes;

    internal override PostgresMessageBase ReadResponseMessage(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        throw new NotImplementedException();
    }
}

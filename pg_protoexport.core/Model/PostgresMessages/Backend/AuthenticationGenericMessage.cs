namespace pg_protoexport;

public class AuthenticationGenericMessage(PostgresMessageDescriptor pgMessage, int length, int data, string commonName) : AuthenticationMessage(pgMessage, length)
{
    internal virtual PostgresMessageBase ReadResponseMessage(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader) { throw new NotImplementedException(); }

    public int Data { get; } = data;
    public string AuthenticationName { get; } = commonName;
}

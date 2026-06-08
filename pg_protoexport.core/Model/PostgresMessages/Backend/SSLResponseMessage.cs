namespace pg_protoexport;

public class SSLResponseMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public bool Accepted { get; init; }

    internal static SSLResponseMessage Read(PostgresMessageDescriptor pgMessage, bool accepted)
    {
        return new SSLResponseMessage(pgMessage, 1)
        {
            Accepted = accepted
        };
    }
}

namespace pg_protoexport;

public class GSSENCResponseMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public bool Accepted { get; init; }

    internal static GSSENCResponseMessage Read(PostgresMessageDescriptor pgMessage, bool accepted)
    {
        return new GSSENCResponseMessage(pgMessage, 1)
        {
            Accepted = accepted
        };
    }
}

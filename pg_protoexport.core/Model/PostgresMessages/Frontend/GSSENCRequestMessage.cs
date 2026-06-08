using System.Diagnostics;

namespace pg_protoexport;

public class GSSENCRequestMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public const int MagicCode = 80877104;

    public int Payload { get; init; }

    internal static GSSENCRequestMessage Read(PostgresMessageDescriptor pgMessage, int messageLength, int requestCode)
    {
        Debug.Assert(requestCode == MagicCode);
        return new GSSENCRequestMessage(pgMessage, messageLength)
        {
            Payload = requestCode
        };
    }
}

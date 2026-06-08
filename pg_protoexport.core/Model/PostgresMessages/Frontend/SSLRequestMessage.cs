using System.Diagnostics;

namespace pg_protoexport;

public class SSLRequestMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public const int MagicCode = 80877103;

    public int Payload { get; init; }

    internal static SSLRequestMessage Read(PostgresMessageDescriptor pgMessage, int messageLength, int requestCode)
    {
        Debug.Assert(requestCode == MagicCode);
        return new SSLRequestMessage(pgMessage, messageLength)
        {
            Payload = requestCode
        };
    }
}

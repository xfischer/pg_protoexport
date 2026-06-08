namespace pg_protoexport;

/// <summary>
/// Frontend aborts an in-progress COPY ... FROM with an error message. Server replies with
/// ErrorResponse and ReadyForQuery.
/// </summary>
public class CopyFailMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public string ErrorMessage { get; init; } = "";

    internal static CopyFailMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        string msg;
        using (reader.BeginField("errorMessage")) msg = reader.ReadNullTerminatedString(len);
        return new CopyFailMessage(pgMessage, len) { ErrorMessage = msg };
    }

    public override string GetStringRepresentation() => ErrorMessage;
}

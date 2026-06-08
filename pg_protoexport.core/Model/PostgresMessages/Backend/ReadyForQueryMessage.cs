namespace pg_protoexport;

public class ReadyForQueryMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public char StatusCode { get; init; }
    
    public TransactionStatus Status { get; private set; }

    internal static ReadyForQueryMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();
        char statusCode;
        using (reader.BeginField("statusCode")) statusCode = reader.ReadChar();
        var message = new ReadyForQueryMessage(pgMessage, len)
        {
            StatusCode = statusCode
        };
        message.Status = message.StatusCode switch
        {
            'I' => TransactionStatus.Idle,
            'T' => TransactionStatus.InTransaction,
            'E' => TransactionStatus.TransationInError,
            _ => TransactionStatus.Unknown,
        };

        return message;
    }

    public override string GetStringRepresentation() => Status.ToString();
}

public enum TransactionStatus
{
    Unknown,
    Idle,
    InTransaction,
    TransationInError
}
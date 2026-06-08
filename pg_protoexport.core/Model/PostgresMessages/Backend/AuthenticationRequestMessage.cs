namespace pg_protoexport;

public class AuthenticationMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public int AuthenticationType { get; }

    internal static AuthenticationMessage Read(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        int len;
        using (reader.BeginField("length")) len = reader.ReadInt32();

        if (len == 8)
        {
            int intData;
            using (reader.BeginField("authenticationType")) intData = reader.ReadInt32();
            return intData switch
            {
                0 => new AuthenticationGenericMessage(pgMessage, len, intData, "AuthenticationOK"),
                2 => new AuthenticationGenericMessage(pgMessage, len, intData, "AuthenticationKerberosV5"),
                3 => new AuthenticationGenericMessage(pgMessage, len, intData, "AuthenticationClearText"),
                7 => new AuthenticationGenericMessage(pgMessage, len, intData, "AuthenticationGSS"),
                9 => new AuthenticationGenericMessage(pgMessage, len, intData, "AuthenticationSSPI"),
                _ => throw new pg_protoexportException($"Invalid Authentication packet! Got value {intData} as 2nd int32 field (first was {len} length).")
            };
        }
        else if (len == 12)
        {
            return AuthenticationMD5PasswordMessage.Read(pgMessage, len, reader);
        }
        else
        {
            int intData;
            using (reader.BeginField("authenticationType")) intData = reader.ReadInt32();
            int payloadLength = len - 4 - 4;
            byte[] payload;
            switch (intData)
            {
                case 8:
                    using (reader.BeginField("authData")) payload = reader.ReadBytes(payloadLength);
                    return new AuthenticationGSSContinueMessage(pgMessage, len, intData, payload);
                case 10:
                    return AuthenticationSASLMessage.Read(pgMessage, len, intData, reader);
                case 11:
                    using (reader.BeginField("saslData")) payload = reader.ReadBytes(payloadLength);
                    return new AuthenticationSASLContinueMessage(pgMessage, len, intData, payload);
                case 12:
                    using (reader.BeginField("saslOutcome")) payload = reader.ReadBytes(payloadLength);
                    return new AuthenticationSASLFinalMessage(pgMessage, len, intData, payload);
                default:
                    throw new pg_protoexportException($"Invalid Authentication packet! Got value {intData} as 2nd int32 field. (first was {len} length)");
            }
        }
    }
}

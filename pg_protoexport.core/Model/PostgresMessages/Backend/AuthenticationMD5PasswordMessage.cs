using System.Diagnostics;

namespace pg_protoexport;

public class AuthenticationMD5PasswordMessage(PostgresMessageDescriptor pgMessage, int length) : AuthenticationGenericMessage(pgMessage, length, 0, "AuthenticationMD5Password")
{
    internal static AuthenticationMD5PasswordMessage Read(PostgresMessageDescriptor pgMessage, int length, PcapBinaryReader reader)
    {
        int authType;
        using (reader.BeginField("authenticationType")) authType = reader.ReadInt32();
        var packet = new AuthenticationMD5PasswordMessage(pgMessage, length)
        {
            MD5Check = authType
        };
        Debug.Assert(packet.MD5Check == 5);
        using (reader.BeginField("salt")) packet.Salt = reader.ReadBytes(4);
        return packet;
    }

    internal override PostgresMessageBase ReadResponseMessage(PostgresMessageDescriptor pgMessage, PcapBinaryReader reader)
    {
        throw new NotImplementedException();
    }

    public int MD5Check { get; init; }
    public byte[] Salt { get; private set; } = [];
}

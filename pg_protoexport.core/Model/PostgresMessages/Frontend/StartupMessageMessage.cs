using System.Diagnostics;

namespace pg_protoexport;

public class StartupMessageMessage(PostgresMessageDescriptor pgMessage, int length) : PostgresMessageBase(pgMessage, length)
{
    public short ProtocolMajorVersion { get; init; }
    public short ProtocolMinorVersion { get; init; }

    public Dictionary<string, string> Parameters { get; set; } = [];

    internal static StartupMessageMessage Read(PostgresMessageDescriptor pgMessage, int length, int protocolVersion, PcapBinaryReader reader)
    {
        short major = (short)((protocolVersion >> 16) & 0xFFFF);
        short minor = (short)(protocolVersion & 0xFFFF);
        var message = new StartupMessageMessage(pgMessage, length)
        {
            ProtocolMajorVersion = major,
            ProtocolMinorVersion = minor
        };

        int bytesRead = 4 + 2 + 2;

        string? paramName = null;
        int paramIndex = 0;
        while (bytesRead < length - 1)
        {
            string param;
            using (reader.BeginField(paramName is null ? $"parameterName[{paramIndex}]" : $"parameterValue[{paramIndex}]"))
                param = reader.ReadNullTerminatedString(length);
            bytesRead += param.Length;
            bytesRead += 1; // null terminator

            if (paramName is null)
            {
                paramName = param;
            }
            else
            {
                message.Parameters.Add(paramName, param);
                paramName = null;
                paramIndex++;
            }
        }
        var lastByte = reader.ReadByte();
        Debug.Assert(lastByte == 0);

        return message;
    }
    public override string GetStringRepresentation() => $"minor={ProtocolMinorVersion}, major={ProtocolMajorVersion}, " +
        $"Parameters=[{string.Join(", ", Parameters.Select(p => $"{p.Key}: {p.Value}"))}]";
}

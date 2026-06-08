using static pg_protoexport.LatexHelper;

namespace pg_protoexport.Templates;

public partial class AuthenticationGeneric : ITextTransformer
{
    public int Length { get; }
    public string AuthName { get; }
    public int AuthTypeCode { get; }
    public string? Data { get; } = string.Empty;

    public AuthenticationGenericMessage Message { get; }

    public AuthenticationGeneric(AuthenticationGenericMessage message)
    {
        Message = message;
        Length = message.Length;
        AuthName = message.AuthenticationName;
        AuthTypeCode = message.Data;

        Data = message switch
        {
            AuthenticationSASLMessage m => TrimUnescape(string.Join(',', m.Mechanisms), 50),
            AuthenticationSASLContinueMessage m => TrimUnescape("SASLData: " + Convert.ToHexStringLower(m.SASLData), 50),
            AuthenticationSASLFinalMessage m => TrimUnescape("Outcome: " + Convert.ToHexStringLower(m.SASLOutcome), 50),
            { AuthenticationName: "AuthenticationOK"} => null,
            _ => null
        };

        // RawData is a derived conversion that depends on the message subtype — cannot be expressed
        // by templates (no type-switch in T4), so it stays on the Params class.
        RawData = message switch
        {
            AuthenticationSASLMessage m => string.Join(',', m.Mechanisms),
            AuthenticationSASLContinueMessage m => Convert.ToHexStringLower(m.SASLData),
            AuthenticationSASLFinalMessage m => Convert.ToHexStringLower(m.SASLOutcome),
            _ => string.Empty
        };

        // DataBytes = body bytes after the 4-byte length field and the 4-byte auth type code.
        // message.Length excludes the code byte but includes the length field itself.
        DataBytes = Math.Max(0, message.Length - 8);
    }

    public string RawData { get; }
    public int DataBytes { get; }
}

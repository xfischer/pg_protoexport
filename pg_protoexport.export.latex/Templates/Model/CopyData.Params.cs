namespace pg_protoexport.Templates;

public partial class CopyData(CopyDataMessage message) : ITextTransformer
{
    public CopyDataMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public int DataLength { get; } = message.DataLength;
    public string PreviewHex { get; } = System.Convert.ToHexString(message.PreviewBytes);
    public bool Truncated { get; } = message.DataLength > CopyDataMessage.PreviewMaxBytes;
    public bool IsHeader { get; } = message.IsHeader;
    public bool IsTrailer { get; } = message.IsTrailer;
    public bool IsBinary { get; } = message.IsBinaryFormat == true;
    public string SignatureHex { get; } = message.BinaryHeader is { Signature: { } sig } ? System.Convert.ToHexString(sig) : string.Empty;
    public bool SignatureValid { get; } = message.BinaryHeader?.SignatureValid ?? false;
    public string FlagsHex { get; } = message.BinaryHeader is { } h ? "0x" + h.Flags.ToString("X8") : string.Empty;
    public int HeaderExtensionLength { get; } = message.BinaryHeader?.HeaderExtensionLength ?? 0;
    public int HeaderExtensionPreviewLength { get; } = message.BinaryHeader?.HeaderExtensionPreview?.Length ?? 0;
}

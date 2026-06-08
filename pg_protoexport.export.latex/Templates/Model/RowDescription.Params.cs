namespace pg_protoexport.Templates;

public partial class RowDescription(RowDescriptionMessage message) : ITextTransformer
{
    public RowDescriptionMessage Message { get; } = message;
    public int Length { get; } = message.Length;
    public int FieldCount { get; } = message.FieldCount;
    public List<FieldDescription> Fields { get; } = message.FieldDescriptions;
}

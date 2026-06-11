namespace pg_protoexport;

public abstract class PostgresMessageBase(PostgresMessageDescriptor pgMessage, int length)
{
    public char Code => pgMessage.Code;

    public string Name => pgMessage.Name;

    public int Length => length;

    public bool FrontEnd => pgMessage.IsFrontEnd;

    public IReadOnlyList<ParsedField> ParsedFields { get; internal set; } = Array.Empty<ParsedField>();

    /// <summary>
    /// Byte offset where this message starts within its <see cref="PostgresPacket.PayloadData"/>.
    /// Only set when <see cref="PcapPostgresOptions.RecordFieldMetadata"/> is enabled.
    /// </summary>
    public int PayloadOffset { get; internal set; }

    /// <summary>
    /// Total on-the-wire byte count of this message (code + length + body). Only set when
    /// <see cref="PcapPostgresOptions.RecordFieldMetadata"/> is enabled.
    /// </summary>
    public int OnWireLength { get; internal set; }

    public virtual string GetStringRepresentation() => this.GetType().Name;
}

public sealed record ParsedField(string Name, int Offset, int Length, string? DisplayValue = null);
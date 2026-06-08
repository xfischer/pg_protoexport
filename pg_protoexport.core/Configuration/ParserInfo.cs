namespace pg_protoexport;

/// <summary>
/// Current parser state, with <see cref="Reader">PcapBinaryReader</see> to work with packet data and other utility properties
/// </summary>
public record struct ParserInfo(PcapBinaryReader Reader, PcapReadState State)
{
    public bool IsFrontEnd { get; set; } = false;

    /// <summary>
    /// Port used on client end, used to distinguish conversations among different connections
    /// </summary>
    public ushort ClientPort { get; set; } = 0;
}

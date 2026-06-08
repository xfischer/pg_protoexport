namespace pg_protoexport;

public sealed record HtmlMessageCard(
    int Idx,
    int PacketIndex,
    string Direction,
    string Code,
    string Name,
    int LengthBytes,
    string Headline,
    List<HtmlField> Fields,
    string? Rationale,
    HtmlStateSnapshot StateAfter)
{
    /// <summary>
    /// If this card represents a CancelRequest whose (pid, secret) matches a BackendKeyData
    /// emitted earlier in the capture, this is the Idx of the first card from that target
    /// conversation. The HTML renders a "Jump to query session" link to that idx.
    /// </summary>
    public int? CorrelatedCardIdx { get; init; }
}

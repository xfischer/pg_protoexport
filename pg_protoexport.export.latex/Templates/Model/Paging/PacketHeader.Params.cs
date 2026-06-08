namespace pg_protoexport.Templates;

public partial class PacketHeader
{
    public string Direction { get; }
    public int PacketIndex { get; }
    public string MessagesCount { get; }
    public GenerationState State { get; }

    public PacketHeader(List<PostgresMessageBase> messages, bool isFrontEnd, int packetIndex, GenerationState state)
    {
        Direction = LatexHelper.GetProtoDirectionText(isFrontEnd);
        PacketIndex = packetIndex;
        State = state;
        MessagesCount = messages.Count == 1 ? "message:1" : $"messages:{messages.Count}";
    }
    public PacketHeader(GenerationState state)
    {
        Direction = LatexHelper.Unescape("- continued -");
        PacketIndex = 0;
        State = state;
        MessagesCount = "";
    }
}

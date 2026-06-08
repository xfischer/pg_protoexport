namespace pg_protoexport.Templates;

public partial class PacketFooter(bool newChapter, GenerationState state, string sectionText = "Conversation")
{
    public bool NewChapter { get; } = newChapter;
    public string SectionText { get; } = sectionText;
    public GenerationState State { get; } = state;
}

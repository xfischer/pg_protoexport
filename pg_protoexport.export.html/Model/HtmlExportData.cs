namespace pg_protoexport;

public sealed record HtmlExportData(
    string Title,
    string GeneratedAt,
    List<string> MermaidDiagrams,
    List<HtmlMessageCard> Cards,
    List<HtmlInterlude> Interludes,
    Dictionary<string, HtmlGlossaryEntry> Glossary,
    Dictionary<string, string> Rationales);

public sealed record HtmlGlossaryEntry(string Definition, string? DocLink, string? SrcLink = null);

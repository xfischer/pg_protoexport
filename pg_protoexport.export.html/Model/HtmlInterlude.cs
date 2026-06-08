namespace pg_protoexport;

public sealed record HtmlInterlude(
    string PatternId,
    int InsertBeforeCardIdx,
    string Title,
    string Body);

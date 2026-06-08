namespace pg_protoexport;

public sealed record InterludeEntry(string Title, string Body);

public sealed class InterludeCatalog
{
    private readonly Dictionary<string, InterludeEntry> _entries;

    public InterludeCatalog(Dictionary<string, InterludeEntry> entries)
    {
        _entries = entries;
    }

    public static InterludeCatalog Empty { get; } = new(new Dictionary<string, InterludeEntry>());

    public bool TryGet(string patternId, out InterludeEntry entry)
    {
        if (_entries.TryGetValue(patternId, out var found))
        {
            entry = found;
            return true;
        }
        entry = null!;
        return false;
    }
}

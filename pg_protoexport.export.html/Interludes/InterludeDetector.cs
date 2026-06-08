namespace pg_protoexport;

/// <summary>
/// Detects protocol patterns in a message stream and emits a prose interlude for each
/// pattern (one per pattern, at most). The <see cref="HtmlInterlude.InsertBeforeCardIdx"/>
/// matches the card index used by <see cref="PcapToHtmlService"/>.
/// </summary>
public static class InterludeDetector
{
    public static List<HtmlInterlude> Detect(IReadOnlyList<PostgresMessageBase> messages, InterludeCatalog catalog)
    {
        var result = new List<HtmlInterlude>();

        TryAdd(result, catalog, "startup", FindAfterFirstReadyForQuery(messages));
        TryAdd(result, catalog, "scram_exchange", FindAfterScramSuccess(messages));
        TryAdd(result, catalog, "extended_query_batch", FindExtendedQueryBatchStart(messages));
        TryAdd(result, catalog, "simple_query_cycle", FindFirst(messages, m => m is QueryMessage));
        TryAdd(result, catalog, "error_recovery", FindFirst(messages, m => m is ErrorResponseMessage));

        result.Sort((a, b) => a.InsertBeforeCardIdx.CompareTo(b.InsertBeforeCardIdx));
        return result;
    }

    private static int? FindAfterFirstReadyForQuery(IReadOnlyList<PostgresMessageBase> messages)
    {
        bool sawStartupEvidence = false;
        for (int i = 0; i < messages.Count; i++)
        {
            if (messages[i] is StartupMessageMessage or SSLRequestMessage or GSSENCRequestMessage or AuthenticationGenericMessage or BackendKeyDataMessage)
                sawStartupEvidence = true;
            if (sawStartupEvidence && messages[i] is ReadyForQueryMessage)
                return i + 1 < messages.Count ? i + 1 : null;
        }
        return null;
    }

    private static int? FindAfterScramSuccess(IReadOnlyList<PostgresMessageBase> messages)
    {
        bool sawSasl = false;
        for (int i = 0; i < messages.Count; i++)
        {
            if (messages[i] is AuthenticationSASLMessage or AuthenticationSASLContinueMessage or AuthenticationSASLFinalMessage)
                sawSasl = true;
            if (sawSasl && messages[i] is AuthenticationGenericMessage auth && auth.AuthenticationName == "AuthenticationOK")
                return i + 1 < messages.Count ? i + 1 : null;
        }
        return null;
    }

    private static int? FindExtendedQueryBatchStart(IReadOnlyList<PostgresMessageBase> messages)
    {
        for (int i = 0; i < messages.Count; i++)
        {
            if (messages[i] is not ParseMessage) continue;
            for (int j = i + 1; j < messages.Count && j <= i + 8; j++)
            {
                if (!messages[j].FrontEnd) break;
                if (messages[j] is BindMessage or SyncMessage)
                    return i;
            }
        }
        return null;
    }

    private static int? FindFirst(IReadOnlyList<PostgresMessageBase> messages, Func<PostgresMessageBase, bool> pred)
    {
        for (int i = 0; i < messages.Count; i++)
            if (pred(messages[i])) return i;
        return null;
    }

    private static void TryAdd(List<HtmlInterlude> sink, InterludeCatalog catalog, string patternId, int? insertBeforeCardIdx)
    {
        if (insertBeforeCardIdx is null) return;
        if (!catalog.TryGet(patternId, out var entry)) return;
        sink.Add(new HtmlInterlude(patternId, insertBeforeCardIdx.Value, entry.Title, entry.Body));
    }
}

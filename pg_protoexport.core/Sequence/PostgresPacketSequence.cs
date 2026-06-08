namespace pg_protoexport;

/// <summary>
/// Format-agnostic sequence-diagram helpers shared by exporters.
/// </summary>
public static class PostgresPacketSequence
{
    /// <summary>
    /// Collapses consecutive same-code messages within a packet into (message, count) groups.
    /// The first occurrence of each run is kept as the representative message.
    /// </summary>
    public static List<(PostgresMessageBase Message, int Count)> GroupMessages(PostgresPacket packet)
    {
        List<(PostgresMessageBase Message, int Count)> grouped = [];
        foreach (var message in packet.Messages)
        {
            if (grouped.Count > 0 && grouped[^1].Message.Code == message.Code)
            {
                grouped[^1] = (grouped[^1].Message, grouped[^1].Count + 1);
            }
            else
            {
                grouped.Add((message, 1));
            }
        }
        return grouped;
    }

    /// <summary>
    /// Builds format-agnostic sequence lines from a packet:
    /// merges consecutive same-direction single-count groups into " / "-joined labels
    /// (e.g. "Parse / Bind / Describe / Execute / Sync"), keeps repeated messages on their
    /// own line ("Foo (x3)"), and flags <see cref="ReadyForQueryMessage"/> and
    /// <see cref="TerminateMessage"/> as session boundaries (ClosesDiagram=true).
    /// </summary>
    public static List<(bool FrontEnd, string Label, bool ClosesDiagram)> BuildSequenceLines(PostgresPacket packet)
    {
        List<(bool FrontEnd, string Label, bool ClosesDiagram)> lines = [];
        List<string> pendingLabels = [];
        bool pendingFrontEnd = false;

        foreach (var (message, count) in GroupMessages(packet))
        {
            bool isBoundary = message is ReadyForQueryMessage or TerminateMessage;
            bool canMerge = count == 1 && !isBoundary;

            if (pendingLabels.Count > 0 && (message.FrontEnd != pendingFrontEnd || !canMerge))
            {
                lines.Add((pendingFrontEnd, string.Join(" / ", pendingLabels), false));
                pendingLabels.Clear();
            }

            if (canMerge)
            {
                pendingFrontEnd = message.FrontEnd;
                pendingLabels.Add(message.Name);
            }
            else
            {
                string label = count == 1 ? message.Name : $"{message.Name} (x{count})";
                lines.Add((message.FrontEnd, label, isBoundary));
            }
        }

        if (pendingLabels.Count > 0)
        {
            lines.Add((pendingFrontEnd, string.Join(" / ", pendingLabels), false));
        }

        return lines;
    }
}

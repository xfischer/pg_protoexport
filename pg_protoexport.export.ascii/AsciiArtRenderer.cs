using System.Text;

namespace pg_protoexport;

/// <summary>
/// Renders a <see cref="PostgresMessageBase"/> as a sequence of labelled boxes — one box per
/// parsed field, sized to fit its full name and value (plus an "(N bytes)" annotation when the
/// field is more than one byte wide). Cells pack left-to-right; the renderer wraps to a new
/// row when adding the next cell would exceed <c>maxLineWidth</c> characters.
///
/// No byte ruler, no offset column — the layout is content-driven; the "(N bytes)" annotation
/// carries the wire-position information.
/// </summary>
internal static class AsciiArtRenderer
{
    public static void RenderMessage(TextWriter w, PostgresMessageBase m, int maxLineWidth)
    {
        if (maxLineWidth < 40) maxLineWidth = 40;
        if (maxLineWidth > 400) maxLineWidth = 400;

        WriteHeader(w, m);
        WriteFields(w, m.ParsedFields, EffectiveSize(m), maxLineWidth);
    }

    public static void WriteHeader(TextWriter w, PostgresMessageBase m)
    {
        var direction = m.FrontEnd ? "F->B" : "B->F";
        w.WriteLine($"[{direction}] {m.Name} ({EffectiveSize(m)} bytes)");
    }

    public static void WriteFields(TextWriter w, IReadOnlyList<ParsedField> fields, int totalBytes, int maxLineWidth)
    {
        if (fields == null || fields.Count == 0)
        {
            WriteSingleBox(w, $"({totalBytes} bytes, unparsed)");
            return;
        }

        var cells = BuildCells(fields);
        var rows = PackIntoRows(cells, maxLineWidth);

        foreach (var row in rows)
        {
            WriteEdge(w, row);
            WriteContentLine(w, row, c => c.Name);
            WriteContentLine(w, row, c => c.Value);
            WriteEdge(w, row);
        }
    }

    /// <summary>
    /// Renders the capture as an ASCII sequence diagram: two lifelines (Client on the left, Server
    /// on the right) with labelled arrows. Frontend traffic points right (<c>--&gt;</c>), backend
    /// traffic points left (<c>&lt;--</c>). A session boundary (ReadyForQuery / Terminate) is
    /// followed by a blank separator line, the ASCII analog of splitting into multiple Mermaid
    /// diagrams. Lines are split with <see cref="BuildAsciiSequenceLines"/> to keep batched flows
    /// readable rather than collapsing a whole packet into one giant arrow.
    /// </summary>
    public static void RenderSequenceDiagram(TextWriter w, IEnumerable<PostgresPacket> packets, int maxLineWidth)
    {
        if (maxLineWidth < 40) maxLineWidth = 40;
        if (maxLineWidth > 400) maxLineWidth = 400;

        // First pass: resolve endpoints and collect every (direction, label, closes) line so we can
        // size the channel to the widest label before drawing anything.
        SessionEndpoints? endpoints = null;
        var lines = new List<(bool FrontEnd, string Label, bool ClosesDiagram)>();
        foreach (var packet in packets)
        {
            endpoints ??= SessionEndpoints.FromFirstPacket(packet);
            lines.AddRange(BuildAsciiSequenceLines(packet));
        }

        if (endpoints is null || lines.Count == 0)
        {
            w.WriteLine("(no messages)");
            return;
        }

        string clientLabel = $"Client ({endpoints.Client}:{endpoints.ClientPort})";
        string serverLabel = $"Server ({endpoints.Server}:{endpoints.ServerPort})";

        // Channel = the run of characters between the two lifeline walls. Width it to the longest
        // label plus arrow decoration (" <-- " + " --> " style needs ~6 chars of dashes/spaces),
        // but never let the whole line exceed maxLineWidth. Over-long labels just extend their line.
        int longestLabel = lines.Max(l => l.Label.Length);
        int channel = Math.Max(longestLabel + 8, Math.Max(clientLabel.Length, serverLabel.Length));
        channel = Math.Min(channel, Math.Max(20, maxLineWidth - 4));

        // Lifeline walls sit at column 0 and column (channel + 1): "|" + channel chars + "|".
        WriteLifelineHeader(w, clientLabel, serverLabel, channel);
        WriteGapLine(w, channel);

        for (int i = 0; i < lines.Count; i++)
        {
            var (frontEnd, label, closes) = lines[i];
            WriteArrowLine(w, label, frontEnd, channel);
            if (closes)
            {
                // Session boundary: blank separator unless this is the very last line.
                if (i < lines.Count - 1)
                {
                    WriteGapLine(w, channel);
                    w.WriteLine();
                    WriteGapLine(w, channel);
                }
            }
            else
            {
                WriteGapLine(w, channel);
            }
        }
    }

    /// <summary>
    /// ASCII-specific variant of <see cref="PostgresPacketSequence.BuildSequenceLines"/> that splits
    /// over-long merged runs into readable arrows:
    /// <list type="bullet">
    /// <item>Frontend: break after every <c>Sync</c>, and after every <c>Execute</c> that is not
    /// immediately followed by a <c>Sync</c> (so each statement in a batch gets its own arrow, while
    /// the trailing <c>... / Execute / Sync</c> stays together).</item>
    /// <item>Backend: break after every <c>CommandComplete</c>, and collapse a run of 3+ consecutive
    /// <c>DataRow</c> messages into a single <c>DataRow (xN)</c> element (1–2 stay individual).</item>
    /// </list>
    /// Direction changes and <c>ReadyForQuery</c>/<c>Terminate</c> boundaries flush as before.
    /// </summary>
    internal static List<(bool FrontEnd, string Label, bool ClosesDiagram)> BuildAsciiSequenceLines(PostgresPacket packet)
    {
        List<(bool FrontEnd, string Label, bool ClosesDiagram)> lines = [];
        List<string> pending = [];
        bool pendingFrontEnd = false;

        void Flush()
        {
            if (pending.Count > 0)
            {
                lines.Add((pendingFrontEnd, string.Join(" / ", pending), false));
                pending.Clear();
            }
        }

        var grouped = PostgresPacketSequence.GroupMessages(packet);
        for (int gi = 0; gi < grouped.Count; gi++)
        {
            var (message, count) = grouped[gi];
            bool isBoundary = message is ReadyForQueryMessage or TerminateMessage;

            if (pending.Count > 0 && message.FrontEnd != pendingFrontEnd)
                Flush();

            if (isBoundary)
            {
                Flush();
                string boundaryLabel = count == 1 ? message.Name : $"{message.Name} (x{count})";
                lines.Add((message.FrontEnd, boundaryLabel, true));
                continue;
            }

            pendingFrontEnd = message.FrontEnd;

            // DataRow: collapse only runs of 3+ into "(xN)"; 1–2 stay as individual elements.
            if (message is DataRowMessage)
            {
                if (count >= 3)
                    pending.Add($"DataRow (x{count})");
                else
                    for (int k = 0; k < count; k++) pending.Add("DataRow");
            }
            else
            {
                pending.Add(count == 1 ? message.Name : $"{message.Name} (x{count})");
            }

            if (message.FrontEnd)
            {
                if (message is SyncMessage)
                {
                    Flush();
                }
                else if (message is ExecuteMessage)
                {
                    var next = gi + 1 < grouped.Count ? grouped[gi + 1].Message : null;
                    if (next is not SyncMessage) Flush();
                }
            }
            else if (message is CommandCompleteMessage)
            {
                Flush();
            }
        }

        Flush();
        return lines;
    }

    private static void WriteLifelineHeader(TextWriter w, string clientLabel, string serverLabel, int channel)
    {
        // Client label is left-aligned over the left wall; server label right-aligned over the
        // right wall (which sits at column channel + 1).
        var sb = new StringBuilder();
        sb.Append(clientLabel);
        int rightWallCol = channel + 1;
        int serverStart = rightWallCol - (serverLabel.Length - 1);
        if (serverStart <= sb.Length)
            serverStart = sb.Length + 1; // guarantee at least one space between labels
        if (serverStart > sb.Length)
            sb.Append(' ', serverStart - sb.Length);
        sb.Append(serverLabel);
        w.WriteLine(sb.ToString());
    }

    private static void WriteGapLine(TextWriter w, int channel)
    {
        var sb = new StringBuilder(channel + 2);
        sb.Append('|');
        sb.Append(' ', channel);
        sb.Append('|');
        w.WriteLine(sb.ToString());
    }

    private static void WriteArrowLine(TextWriter w, string label, bool frontEnd, int channel)
    {
        // " Label " centered in the channel, with the surrounding fill drawn as dashes so the
        // result reads as an arrow shaft. Frontend points right (arrowhead at the right wall),
        // backend points left (arrowhead at the left wall).
        string text = $" {label} ";
        if (text.Length > channel)
        {
            // Over-long label: let the line extend; minimal decoration.
            string head = frontEnd ? $"|--{text}-->|" : $"|<--{text}--|";
            w.WriteLine(head);
            return;
        }

        int fill = channel - text.Length;
        int left = fill / 2;
        int right = fill - left;

        var sb = new StringBuilder(channel + 2);
        sb.Append('|');
        if (frontEnd)
        {
            sb.Append('-', left);
            sb.Append(text);
            sb.Append('-', Math.Max(0, right - 1));
            sb.Append('>');
        }
        else
        {
            sb.Append('<');
            sb.Append('-', Math.Max(0, left - 1));
            sb.Append(text);
            sb.Append('-', right);
        }
        sb.Append('|');
        w.WriteLine(sb.ToString());
    }

    private sealed record Cell(string Name, string Value, int InnerWidth);

    private static List<Cell> BuildCells(IReadOnlyList<ParsedField> fields)
    {
        var cells = new List<Cell>(fields.Count);
        foreach (var f in fields)
        {
            string valueText = FormatDisplay(f);
            string fullValue = f.Length > 1
                ? AppendByteAnnotation(valueText, f.Length)
                : valueText;
            int innerWidth = Math.Max(f.Name.Length, fullValue.Length);
            cells.Add(new Cell(f.Name, fullValue, innerWidth));
        }
        return cells;
    }

    private static string AppendByteAnnotation(string valueText, int byteCount)
    {
        string suffix = $"({byteCount} bytes)";
        return string.IsNullOrEmpty(valueText) ? suffix : $"{valueText} {suffix}";
    }

    /// <summary>
    /// Pack cells left-to-right into rows of at most <paramref name="maxLineWidth"/> chars. A row
    /// of N cells produces a line of width <c>1 + sum(innerWidth + 3)</c> — leading <c>|</c> plus
    /// each cell's <c>" content |"</c> (1 padding + content + 1 padding + wall). A single oversized
    /// cell still gets its own row (it just exceeds maxLineWidth — better than truncating).
    /// </summary>
    private static List<List<Cell>> PackIntoRows(List<Cell> cells, int maxLineWidth)
    {
        var rows = new List<List<Cell>> { new() };
        int currentWidth = 1; // leading '|'

        foreach (var cell in cells)
        {
            int cellWidth = cell.InnerWidth + 3; // " " + content + " " + "|"
            if (currentWidth + cellWidth > maxLineWidth && rows[^1].Count > 0)
            {
                rows.Add(new List<Cell>());
                currentWidth = 1;
            }
            rows[^1].Add(cell);
            currentWidth += cellWidth;
        }

        if (rows[^1].Count == 0) rows.RemoveAt(rows.Count - 1);
        return rows;
    }

    private static void WriteEdge(TextWriter w, List<Cell> row)
    {
        var sb = new StringBuilder();
        sb.Append('+');
        foreach (var cell in row)
        {
            sb.Append('-', cell.InnerWidth + 2);
            sb.Append('+');
        }
        w.WriteLine(sb.ToString());
    }

    private static void WriteContentLine(TextWriter w, List<Cell> row, Func<Cell, string> select)
    {
        var sb = new StringBuilder();
        sb.Append('|');
        foreach (var cell in row)
        {
            sb.Append(' ');
            sb.Append(CenterFit(select(cell), cell.InnerWidth));
            sb.Append(' ');
            sb.Append('|');
        }
        w.WriteLine(sb.ToString());
    }

    private static void WriteSingleBox(TextWriter w, string text)
    {
        int inner = text.Length;
        w.WriteLine("+" + new string('-', inner + 2) + "+");
        w.WriteLine($"| {text} |");
        w.WriteLine("+" + new string('-', inner + 2) + "+");
    }

    private static string CenterFit(string? s, int width)
    {
        if (width <= 0) return "";
        s ??= "";
        if (s.Length >= width) return s.Substring(0, width);
        int total = width - s.Length;
        int left = total / 2;
        int right = total - left;
        return new string(' ', left) + s + new string(' ', right);
    }

    internal static string FormatDisplay(ParsedField f)
    {
        var v = f.DisplayValue;
        if (string.IsNullOrEmpty(v)) return "";
        if (LooksLikeInt(v)) return v;
        if (v.Length == 1 && v[0] >= ' ' && v[0] <= '~') return $"'{v}'";
        return EscapeAndQuote(v);
    }

    private static bool LooksLikeInt(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        int i = 0;
        if (s[0] == '-' || s[0] == '+') i = 1;
        if (i >= s.Length) return false;
        for (; i < s.Length; i++)
            if (!char.IsDigit(s[i])) return false;
        return true;
    }

    private static string EscapeAndQuote(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        sb.Append('"');
        foreach (var c in s)
        {
            if (c == '\\') sb.Append("\\\\");
            else if (c == '"') sb.Append("\\\"");
            else if (c == '\0') sb.Append("\\0");
            else if (c == '\n') sb.Append("\\n");
            else if (c == '\r') sb.Append("\\r");
            else if (c == '\t') sb.Append("\\t");
            else if (c < ' ' || c == 0x7F) sb.AppendFormat("\\x{0:X2}", (int)c);
            else sb.Append(c);
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static int EffectiveSize(PostgresMessageBase m) =>
        m.OnWireLength > 0 ? m.OnWireLength : m.Length;
}

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

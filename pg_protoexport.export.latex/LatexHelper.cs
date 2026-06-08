using System.Diagnostics;
using System.Text;

namespace pg_protoexport;

public static class LatexHelper
{
    public static string Unescape(string str)
    {
        return str
            .Replace("\\", "\\textbackslash ")
            .ReplaceLineEndings(" ")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("#", "\\#")
            .Replace("$", "\\$")
            .Replace("%", "\\%")
            .Replace("&", "\\&")
            .Replace("_", "\\_");

    }

    public static string TrimUnescape(string? str, int maxLength)
    {
        if (str == null)
            return string.Empty;

        if (str.Length < maxLength)
            return Unescape(str);
        return Unescape(str[..maxLength]) + "$\\cdots$";
    }

    /// <summary>
    /// Replaces control chars (and 0x7F) with visible LaTeX glyphs. Does NOT escape LaTeX special
    /// chars (braces, #, $, ...). Intended as a building block for <see cref="UnescapeExact"/>.
    /// </summary>
    public static string VisibleControlChars(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var sb = new StringBuilder(raw.Length);
        foreach (char c in raw)
            AppendVisibleControl(sb, c, escapeLatex: false);
        return sb.ToString();
    }

    /// <summary>
    /// Single-pass escape used in <see cref="LatexRenderOptions.Exact"/> mode. Renders every char
    /// either as a LaTeX-escaped literal (for special chars: <c>\</c>, <c>{</c>, <c>}</c>, <c>#</c>,
    /// <c>$</c>, <c>%</c>, <c>&amp;</c>, <c>_</c>) or as a visible glyph (for control chars and 0x7F).
    /// Unlike <see cref="Unescape"/>, no line-ending collapse — newlines render as
    /// <c>\textbackslash n</c> so the byte-exact intent is preserved.
    /// </summary>
    public static string UnescapeExact(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var sb = new StringBuilder(raw.Length);
        foreach (char c in raw)
            AppendVisibleControl(sb, c, escapeLatex: true);
        return sb.ToString();
    }

    private static void AppendVisibleControl(StringBuilder sb, char c, bool escapeLatex)
    {
        switch (c)
        {
            case '\\': sb.Append("\\textbackslash "); return;
            case '\n': sb.Append("\\textbackslash n"); return;
            case '\r': sb.Append("\\textbackslash r"); return;
            case '\t': sb.Append("\\textbackslash t"); return;
            case '\0': sb.Append("\\textbackslash 0"); return;
        }

        if (c < 0x20 || c == 0x7F)
        {
            sb.Append("\\textbackslash x").Append(((int)c).ToString("X2"));
            return;
        }

        if (escapeLatex)
        {
            switch (c)
            {
                case '{': sb.Append("\\{"); return;
                case '}': sb.Append("\\}"); return;
                case '#': sb.Append("\\#"); return;
                case '$': sb.Append("\\$"); return;
                case '%': sb.Append("\\%"); return;
                case '&': sb.Append("\\&"); return;
                case '_': sb.Append("\\_"); return;
            }
        }

        sb.Append(c);
    }

    /// <summary>
    /// Emits the LaTeX <c>\bitbox{N}{...}</c> markup for one logical field, slicing the content into
    /// chunks that respect the bytefield <paramref name="rowWidth"/>. UTF-8 codepoints are never split.
    /// If <paramref name="byteCount"/> exceeds the UTF-8 length of <paramref name="rawContent"/>, the
    /// remainder is rendered as <c>\0</c> glyphs (null-terminator bytes).
    /// </summary>
    /// <param name="label">Optional label prepended to the first chunk (e.g. <c>"query: "</c>).</param>
    /// <param name="rawContent">The unescaped string content of the field.</param>
    /// <param name="byteCount">Total on-the-wire byte count of the field (incl. null terminator).</param>
    /// <param name="rowWidth">Bytefield row width in bytes.</param>
    /// <param name="usedBytes">Running byte count on the current row; mutated by this call.</param>
    /// <param name="bgcolor">Optional <c>bgcolor=</c> applied to every emitted bitbox.</param>
    /// <param name="emitLeadingSeparator">When true (the default), the helper emits its own leading
    /// <c>&amp;</c> separator if <paramref name="usedBytes"/> &gt; 0 — so callers should NOT write a
    /// <c>&amp;</c> immediately before invoking this helper.</param>
    public static string EmitWrappingBitboxes(
        string? label,
        string rawContent,
        int byteCount,
        int rowWidth,
        ref int usedBytes,
        string? bgcolor = null,
        bool emitLeadingSeparator = true)
    {
        if (rowWidth < 4)
            throw new ArgumentOutOfRangeException(nameof(rowWidth), rowWidth, "rowWidth must be at least 4 to accommodate UTF-8 codepoints.");
        if (byteCount <= 0)
            return string.Empty;

        byte[] contentBytes = string.IsNullOrEmpty(rawContent)
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(rawContent);
        int contentLen = contentBytes.Length;

        var sb = new StringBuilder();
        int remaining = byteCount;
        int contentPos = 0;
        bool isFirstChunk = true;
        bool needSeparator = emitLeadingSeparator && usedBytes > 0;
        string bg = string.IsNullOrEmpty(bgcolor) ? string.Empty : $"[bgcolor={bgcolor}]";

        while (remaining > 0)
        {
            int spaceLeftOnRow = rowWidth - usedBytes;
            if (spaceLeftOnRow <= 0)
            {
                sb.Append(" \\\\").AppendLine();
                usedBytes = 0;
                spaceLeftOnRow = rowWidth;
                needSeparator = false;
            }

            int chunkBytes = Math.Min(remaining, spaceLeftOnRow);

            int contentBytesInChunk = Math.Min(chunkBytes, contentLen - contentPos);
            int nullBytesInChunk = chunkBytes - contentBytesInChunk;

            // Avoid splitting a UTF-8 codepoint: if more content remains AFTER this chunk and the
            // byte at position (contentPos + contentBytesInChunk) is a continuation byte, back up.
            if (contentBytesInChunk > 0 && contentPos + contentBytesInChunk < contentLen)
            {
                while (contentBytesInChunk > 0 && IsUtf8Continuation(contentBytes[contentPos + contentBytesInChunk]))
                {
                    contentBytesInChunk--;
                }

                // If we couldn't fit even one full codepoint, wrap to a new row and retry.
                if (contentBytesInChunk == 0)
                {
                    if (spaceLeftOnRow == rowWidth)
                    {
                        // Fresh row already, and we still can't fit. Force-emit the next codepoint
                        // anyway (slightly oversized cell — should never happen for rowWidth >= 8).
                        contentBytesInChunk = NextCodepointLength(contentBytes, contentPos);
                        nullBytesInChunk = 0;
                    }
                    else
                    {
                        sb.Append(" \\\\").AppendLine();
                        usedBytes = 0;
                        needSeparator = false;
                        continue;
                    }
                }
            }

            int actualChunkSize = contentBytesInChunk + nullBytesInChunk;

            string chunkText = contentBytesInChunk > 0
                ? Encoding.UTF8.GetString(contentBytes, contentPos, contentBytesInChunk)
                : string.Empty;
            string escapedChunk = UnescapeExact(chunkText);
            if (nullBytesInChunk > 0)
            {
                for (int i = 0; i < nullBytesInChunk; i++)
                    escapedChunk += "\\textbackslash 0";
            }

            string labelText = isFirstChunk && !string.IsNullOrEmpty(label)
                ? UnescapeExact(label)
                : string.Empty;

            if (needSeparator)
                sb.Append(" & ");
            sb.Append("\\bitbox{").Append(actualChunkSize).Append('}').Append(bg)
              .Append('{').Append(labelText).Append(escapedChunk).Append('}');

            contentPos += contentBytesInChunk;
            usedBytes += actualChunkSize;
            remaining -= actualChunkSize;
            isFirstChunk = false;
            needSeparator = true;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Emits one <c>\bitbox{byteCount}{label}</c> for a fixed-width structural field (an int, a code,
    /// a count). Unlike <see cref="EmitWrappingBitboxes"/>, the content is treated as a pure label
    /// (no trailing null glyphs). If the field would not fit on the current row, this helper pads
    /// the row with a gray bitbox, emits <c>\\</c>, and starts a new row.
    /// </summary>
    public static string EmitFixedBitbox(
        string label,
        int byteCount,
        int rowWidth,
        ref int usedBytes,
        string? bgcolor = null,
        bool emitLeadingSeparator = true)
    {
        if (byteCount <= 0) return string.Empty;
        if (byteCount > rowWidth)
            throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount, $"Field is wider than the row width ({rowWidth}).");

        var sb = new StringBuilder();
        string bg = string.IsNullOrEmpty(bgcolor) ? string.Empty : $"[bgcolor={bgcolor}]";

        if (rowWidth - usedBytes < byteCount)
        {
            if (usedBytes > 0 && usedBytes < rowWidth)
                sb.Append(" & \\bitbox{").Append(rowWidth - usedBytes).Append("}[bgcolor=lightgray]{}");
            sb.Append(" \\\\").AppendLine();
            usedBytes = 0;
        }
        else if (emitLeadingSeparator && usedBytes > 0)
        {
            sb.Append(" & ");
        }

        sb.Append("\\bitbox{").Append(byteCount).Append('}').Append(bg)
          .Append('{').Append(UnescapeExact(label)).Append('}');
        usedBytes += byteCount;
        return sb.ToString();
    }

    /// <summary>
    /// Emits a trailing gray <c>\bitbox{rowWidth - usedBytes}[bgcolor=lightgray]{}</c> if the current
    /// row is partially filled, padding it to exactly <paramref name="rowWidth"/> bytes wide.
    /// </summary>
    public static string EmitPadToRowEnd(ref int usedBytes, int rowWidth, bool leadingSeparator = true)
    {
        if (usedBytes <= 0 || usedBytes >= rowWidth)
            return string.Empty;

        int pad = rowWidth - usedBytes;
        usedBytes = rowWidth;
        string sep = leadingSeparator ? " & " : string.Empty;
        return $"{sep}\\bitbox{{{pad}}}[bgcolor=lightgray]{{}}";
    }

    /// <summary>
    /// Page-break heuristic for <see cref="LatexRenderOptions.Exact"/> mode: the number of bytefield
    /// rows a message of <paramref name="totalMessageBytes"/> bytes takes at <paramref name="rowWidth"/>.
    /// Always at least 1.
    /// </summary>
    public static float CountExactRows(int totalMessageBytes, int rowWidth)
    {
        if (totalMessageBytes <= 0)
            return 1f;
        return (float)Math.Ceiling((double)totalMessageBytes / rowWidth);
    }

    /// <summary>
    /// Same as <see cref="EmitWrappingBitboxes"/> but auto-computes the byte count for a
    /// null-terminated UTF-8 C-string: <c>UTF8(raw).Length + 1</c>.
    /// </summary>
    public static string EmitWrappingCString(
        string? label,
        string? raw,
        int rowWidth,
        ref int usedBytes,
        string? bgcolor = null)
    {
        string content = raw ?? string.Empty;
        int byteCount = Encoding.UTF8.GetByteCount(content) + 1;
        return EmitWrappingBitboxes(label, content, byteCount, rowWidth, ref usedBytes, bgcolor);
    }

    /// <summary>
    /// <see cref="ITextTransformer.EstimateBytefieldRowCount"/> helper. Combines <paramref name="messageLength"/>
    /// (the value from the PostgreSQL wire <c>Length</c> field, which excludes the 1-byte code) with the
    /// implicit code byte. Pass <paramref name="hasCodeByte"/>=false for code-less messages (StartupMessage,
    /// SSLRequest).
    /// </summary>
    public static float CountExactRowsForMessage(int messageLength, int rowWidth, bool hasCodeByte = true)
        => CountExactRows(messageLength + (hasCodeByte ? 1 : 0), rowWidth);

    private static bool IsUtf8Continuation(byte b) => (b & 0xC0) == 0x80;

    private static int NextCodepointLength(byte[] bytes, int pos)
    {
        if (pos >= bytes.Length) return 0;
        byte b = bytes[pos];
        if ((b & 0x80) == 0) return 1;
        if ((b & 0xE0) == 0xC0) return 2;
        if ((b & 0xF0) == 0xE0) return 3;
        if ((b & 0xF8) == 0xF0) return 4;
        return 1; // malformed; consume one byte to make progress
    }

    public static string ToFormatString(short format) =>
        format switch
        {
            0 => "Text",
            1 => "Binary",
            _ => "Unknown"
        };
   
    public static string ParamDirection(short value) =>
        value switch
        {
            1 => "IN",
            2 => "OUT",
            3 => "INOUT",
            _ => "??"
        };

    public static string GetProtoDirectionText(bool? isFrontEnd)
    {
        if (isFrontEnd == null)
            return "Unknown";

        return isFrontEnd.Value ? "\\underline{FrontEnd}$\\longrightarrow$BackEnd" : "FrontEnd$\\longleftarrow$\\underline{BackEnd}";
    }


    [Conditional("DEBUG")]
    public static void AppendLineIfDebug(this StringBuilder builder, string content)
    {
        builder.AppendLine(content);
    }

}

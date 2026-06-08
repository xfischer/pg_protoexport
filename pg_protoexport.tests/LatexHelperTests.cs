namespace pg_protoexport.tests;

public class LatexHelperTests
{
    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("a_b", "a\\_b")]
    [InlineData("a&b", "a\\&b")]
    [InlineData("a$b", "a\\$b")]
    [InlineData("a%b", "a\\%b")]
    [InlineData("a#b", "a\\#b")]
    [InlineData("a{b}", "a\\{b\\}")]
    [InlineData("a\\b", "a\\textbackslash b")]
    [InlineData("line1\r\nline2", "line1 line2")]
    public void Unescape_EscapesSpecialCharacters(string input, string expected)
    {
        var result = LatexHelper.Unescape(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("short", 10, "short")]
    [InlineData("this is a long string", 10, "this is a $\\cdots$")]
    [InlineData(null, 10, "")]
    public void TrimUnescape_TruncatesAndEscapes(string? input, int maxLength, string expected)
    {
        var result = LatexHelper.TrimUnescape(input, maxLength);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData((short)0, "Text")]
    [InlineData((short)1, "Binary")]
    [InlineData((short)99, "Unknown")]
    public void ToFormatString_ReturnsCorrectLabel(short format, string expected)
    {
        Assert.Equal(expected, LatexHelper.ToFormatString(format));
    }

    [Theory]
    [InlineData(true, "FrontEnd")]
    [InlineData(false, "BackEnd")]
    [InlineData(null, "Unknown")]
    public void GetProtoDirectionText_ReturnsCorrectDirection(bool? isFrontEnd, string expectedSubstring)
    {
        var result = LatexHelper.GetProtoDirectionText(isFrontEnd);
        Assert.Contains(expectedSubstring, result);
    }

    // ---- VisibleControlChars ----

    [Theory]
    [InlineData("\n", "\\textbackslash n")]
    [InlineData("\r", "\\textbackslash r")]
    [InlineData("\t", "\\textbackslash t")]
    [InlineData("\0", "\\textbackslash 0")]
    [InlineData("", "\\textbackslash x07")]
    [InlineData("", "\\textbackslash x7F")]
    [InlineData("SELECT", "SELECT")]
    [InlineData("a\nb", "a\\textbackslash nb")]
    public void VisibleControlChars_RendersControlsAsGlyphs(string input, string expected)
    {
        Assert.Equal(expected, LatexHelper.VisibleControlChars(input));
    }

    [Fact]
    public void VisibleControlChars_DoesNotEscapeLatexSpecials()
    {
        // LaTeX special chars are left untouched here (UnescapeExact handles them).
        Assert.Equal("a{b}c#d$e%f&g_h", LatexHelper.VisibleControlChars("a{b}c#d$e%f&g_h"));
    }

    // ---- UnescapeExact ----

    [Fact]
    public void UnescapeExact_EscapesLatexSpecialsAndControls()
    {
        Assert.Equal(
            "\\{a\\}\\#\\$\\%\\&\\_\\textbackslash n",
            LatexHelper.UnescapeExact("{a}#$%&_\n"));
    }

    [Fact]
    public void UnescapeExact_PreservesPlainAscii()
    {
        Assert.Equal("SELECT * FROM users", LatexHelper.UnescapeExact("SELECT * FROM users"));
    }

    [Fact]
    public void UnescapeExact_BackslashBecomesTextbackslash()
    {
        Assert.Equal("\\textbackslash path", LatexHelper.UnescapeExact("\\path"));
    }

    // ---- CountExactRows ----

    [Theory]
    [InlineData(0, 32, 1f)]
    [InlineData(1, 32, 1f)]
    [InlineData(32, 32, 1f)]
    [InlineData(33, 32, 2f)]
    [InlineData(70, 32, 3f)]
    [InlineData(65, 32, 3f)]
    [InlineData(128, 64, 2f)]
    public void CountExactRows(int totalBytes, int rowWidth, float expected)
    {
        Assert.Equal(expected, LatexHelper.CountExactRows(totalBytes, rowWidth));
    }

    // ---- EmitWrappingBitboxes ----

    [Fact]
    public void EmitWrappingBitboxes_FitsOnOneRow()
    {
        int usedBytes = 5;
        string result = LatexHelper.EmitWrappingBitboxes(
            label: null,
            rawContent: "0123456789",
            byteCount: 10,
            rowWidth: 32,
            usedBytes: ref usedBytes);

        Assert.Equal(15, usedBytes);
        Assert.Equal(" & \\bitbox{10}{0123456789}", result);
    }

    [Fact]
    public void EmitWrappingBitboxes_NoLeadingSeparatorWhenRowEmpty()
    {
        int usedBytes = 0;
        string result = LatexHelper.EmitWrappingBitboxes(
            label: null,
            rawContent: "abc",
            byteCount: 3,
            rowWidth: 32,
            usedBytes: ref usedBytes);

        Assert.Equal("\\bitbox{3}{abc}", result);
    }

    [Fact]
    public void EmitWrappingBitboxes_LeadingSeparatorSuppressed()
    {
        int usedBytes = 5;
        string result = LatexHelper.EmitWrappingBitboxes(
            label: null,
            rawContent: "abc",
            byteCount: 3,
            rowWidth: 32,
            usedBytes: ref usedBytes,
            emitLeadingSeparator: false);

        Assert.Equal("\\bitbox{3}{abc}", result);
    }

    [Fact]
    public void EmitWrappingBitboxes_WrapsOnce()
    {
        int usedBytes = 5;
        // 30 ASCII bytes; 27 bytes fit on the current row (5 + 27 = 32), 3 spill to the next row.
        string content = new string('x', 30);
        string result = LatexHelper.EmitWrappingBitboxes(
            label: null,
            rawContent: content,
            byteCount: 30,
            rowWidth: 32,
            usedBytes: ref usedBytes);

        Assert.Equal(3, usedBytes);
        Assert.StartsWith(" & ", result);
        Assert.Contains("\\bitbox{27}{" + new string('x', 27) + "}", result);
        Assert.Contains("\\\\", result);
        Assert.Contains("\\bitbox{3}{" + new string('x', 3) + "}", result);
    }

    [Fact]
    public void EmitWrappingBitboxes_WrapsMultipleRows()
    {
        int usedBytes = 5;
        // 65 ASCII bytes -> chunks of 27, 32, 6. Last chunk leaves usedBytes = 6.
        string content = new string('y', 65);
        string result = LatexHelper.EmitWrappingBitboxes(
            label: null,
            rawContent: content,
            byteCount: 65,
            rowWidth: 32,
            usedBytes: ref usedBytes);

        Assert.Equal(6, usedBytes);
        Assert.StartsWith(" & ", result);
        Assert.Contains("\\bitbox{27}{" + new string('y', 27) + "}", result);
        Assert.Contains("\\bitbox{32}{" + new string('y', 32) + "}", result);
        Assert.Contains("\\bitbox{6}{" + new string('y', 6) + "}", result);
        // Two row breaks between three chunks.
        Assert.Equal(2, CountOccurrences(result, "\\\\"));
    }

    [Fact]
    public void EmitWrappingBitboxes_NullTerminatorRenderedAsGlyph()
    {
        // 1-char string + null terminator (byteCount=2).
        int usedBytes = 0;
        string result = LatexHelper.EmitWrappingBitboxes(
            label: null,
            rawContent: "X",
            byteCount: 2,
            rowWidth: 32,
            usedBytes: ref usedBytes);

        Assert.Equal(2, usedBytes);
        Assert.Equal("\\bitbox{2}{X\\textbackslash 0}", result);
    }

    [Fact]
    public void EmitWrappingBitboxes_EmptyContentWithNullByte()
    {
        // Empty string + null terminator (e.g. an unnamed Parse statement).
        int usedBytes = 5;
        string result = LatexHelper.EmitWrappingBitboxes(
            label: "statement: ",
            rawContent: "",
            byteCount: 1,
            rowWidth: 32,
            usedBytes: ref usedBytes);

        Assert.Equal(6, usedBytes);
        Assert.Equal(" & \\bitbox{1}{statement: \\textbackslash 0}", result);
    }

    [Fact]
    public void EmitWrappingBitboxes_PreservesMultiByteUtf8AtChunkBoundary()
    {
        // "中" is 3 UTF-8 bytes (0xE4 0xB8 0xAD).
        // usedBytes=30, rowWidth=32 -> spaceLeftOnRow=2; the codepoint must be deferred to the next row.
        int usedBytes = 30;
        string result = LatexHelper.EmitWrappingBitboxes(
            label: null,
            rawContent: "中a",
            byteCount: 4,    // 3 bytes for 中 + 1 byte for 'a'
            rowWidth: 32,
            usedBytes: ref usedBytes);

        // The 中 codepoint must appear whole on the second (wrapped) row, alongside the 'a'.
        Assert.Contains("\\bitbox{4}{中a}", result);
        // And a row break between this chunk and the previous (empty) state — wrap happened.
        Assert.Contains("\\\\", result);
    }

    [Fact]
    public void EmitWrappingBitboxes_BgColorAppliedToEveryChunk()
    {
        int usedBytes = 0;
        string content = new string('a', 40);
        string result = LatexHelper.EmitWrappingBitboxes(
            label: null,
            rawContent: content,
            byteCount: 40,
            rowWidth: 32,
            usedBytes: ref usedBytes,
            bgcolor: "lightgreen");

        Assert.Equal(2, CountOccurrences(result, "[bgcolor=lightgreen]"));
    }

    [Fact]
    public void EmitWrappingBitboxes_ZeroByteCountEmitsNothing()
    {
        int usedBytes = 5;
        string result = LatexHelper.EmitWrappingBitboxes(
            label: "ignored: ",
            rawContent: "ignored",
            byteCount: 0,
            rowWidth: 32,
            usedBytes: ref usedBytes);

        Assert.Equal("", result);
        Assert.Equal(5, usedBytes);
    }

    [Fact]
    public void EmitWrappingBitboxes_RejectsTooSmallRowWidth()
    {
        int usedBytes = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            int u = usedBytes;
            LatexHelper.EmitWrappingBitboxes(null, "x", 1, rowWidth: 3, usedBytes: ref u);
        });
    }

    // ---- EmitPadToRowEnd ----

    [Fact]
    public void EmitPadToRowEnd_PadsPartialRow()
    {
        int usedBytes = 6;
        string result = LatexHelper.EmitPadToRowEnd(ref usedBytes, rowWidth: 32);

        Assert.Equal(32, usedBytes);
        Assert.Equal(" & \\bitbox{26}[bgcolor=lightgray]{}", result);
    }

    [Fact]
    public void EmitPadToRowEnd_NoOpWhenRowFull()
    {
        int usedBytes = 32;
        string result = LatexHelper.EmitPadToRowEnd(ref usedBytes, rowWidth: 32);

        Assert.Equal("", result);
        Assert.Equal(32, usedBytes);
    }

    [Fact]
    public void EmitPadToRowEnd_NoOpWhenRowEmpty()
    {
        int usedBytes = 0;
        string result = LatexHelper.EmitPadToRowEnd(ref usedBytes, rowWidth: 32);

        Assert.Equal("", result);
        Assert.Equal(0, usedBytes);
    }

    [Fact]
    public void EmitPadToRowEnd_OmitsLeadingSeparatorOnRequest()
    {
        int usedBytes = 6;
        string result = LatexHelper.EmitPadToRowEnd(ref usedBytes, rowWidth: 32, leadingSeparator: false);
        Assert.Equal("\\bitbox{26}[bgcolor=lightgray]{}", result);
    }

    // ---- EmitWrappingCString ----

    [Fact]
    public void EmitWrappingCString_AddsNullTerminatorByte()
    {
        // raw "abc" + null terminator = 4 bytes.
        int usedBytes = 0;
        string result = LatexHelper.EmitWrappingCString(
            label: null, raw: "abc", rowWidth: 32, usedBytes: ref usedBytes);

        Assert.Equal(4, usedBytes);
        Assert.Equal("\\bitbox{4}{abc\\textbackslash 0}", result);
    }

    [Fact]
    public void EmitWrappingCString_EmptyStringRendersOnlyNullTerminator()
    {
        int usedBytes = 5;
        string result = LatexHelper.EmitWrappingCString(
            label: "statement: ", raw: "", rowWidth: 32, usedBytes: ref usedBytes);

        Assert.Equal(6, usedBytes);
        Assert.Equal(" & \\bitbox{1}{statement: \\textbackslash 0}", result);
    }

    [Fact]
    public void EmitWrappingCString_HandlesNullRaw()
    {
        int usedBytes = 0;
        string result = LatexHelper.EmitWrappingCString(
            label: null, raw: null, rowWidth: 32, usedBytes: ref usedBytes);

        // null raw is treated as empty string -> only null terminator
        Assert.Equal(1, usedBytes);
        Assert.Equal("\\bitbox{1}{\\textbackslash 0}", result);
    }

    // ---- CountExactRowsForMessage ----

    [Theory]
    [InlineData(83, 32, true, 3f)]  // 1 + 83 = 84 bytes / 32 = ceil 3
    [InlineData(4, 32, true, 1f)]   // 1 + 4 = 5 bytes -> 1 row
    [InlineData(80, 32, false, 3f)] // no code byte: 80 / 32 = ceil 3
    [InlineData(0, 32, false, 1f)]  // always at least 1
    public void CountExactRowsForMessage(int messageLength, int rowWidth, bool hasCodeByte, float expected)
    {
        Assert.Equal(expected, LatexHelper.CountExactRowsForMessage(messageLength, rowWidth, hasCodeByte));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(needle)) return 0;
        int count = 0;
        int idx = 0;
        while ((idx = haystack.IndexOf(needle, idx)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}

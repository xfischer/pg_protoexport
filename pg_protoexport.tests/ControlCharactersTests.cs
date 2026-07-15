namespace pg_protoexport.tests;

public class ControlCharactersTests
{
    [Theory]
    [InlineData('a', false)]
    [InlineData('~', false)]
    [InlineData(' ', false)]
    [InlineData((char)0x1F, true)]
    [InlineData((char)0x7F, true)]
    [InlineData((char)0x00, true)]
    public void IsControl_ClassifiesC0AndDel(char c, bool expected)
    {
        Assert.Equal(expected, ControlCharacters.IsControl(c));
    }

    [Theory]
    [InlineData(0x00, "NUL")]
    [InlineData(0x02, "STX")]
    [InlineData(0x07, "BEL")]
    [InlineData(0x1F, "US")]
    [InlineData(0x7F, "DEL")]
    public void Mnemonic_ReturnsAsciiName(int codePoint, string expected)
    {
        Assert.Equal(expected, ControlCharacters.Mnemonic((char)codePoint));
    }

    [Fact]
    public void EscapeControlChars_LeavesPlainTextUntouched()
    {
        Assert.Equal("SELECT 1", ControlCharacters.EscapeControlChars("SELECT 1"));
    }

    [Fact]
    public void EscapeControlChars_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ControlCharacters.EscapeControlChars(null));
        Assert.Equal(string.Empty, ControlCharacters.EscapeControlChars(""));
    }

    [Theory]
    [InlineData("\n", "\\n")]
    [InlineData("\r", "\\r")]
    [InlineData("\t", "\\t")]
    [InlineData("\\", "\\\\")]
    public void EscapeControlChars_UsesFriendlyEscapesForCommonBytes(string input, string expected)
    {
        Assert.Equal(expected, ControlCharacters.EscapeControlChars(input));
    }

    // Bytes with no friendly escape (everything except \n, \r, \t, \\) fall back to the ASCII mnemonic,
    // including NUL. Built from code points at runtime so no raw control byte lands in this source file
    // (the same reason LatexHelperTests does this — raw NUL/STX/DEL bytes previously corrupted a
    // generated .tex file).
    [Theory]
    [InlineData(new[] { 0x00 }, "\\NUL")]
    [InlineData(new[] { 0x02 }, "\\STX")]
    [InlineData(new[] { 0x7F }, "\\DEL")]
    [InlineData(new[] { (int)'a', 0x02, (int)'b' }, "a\\STXb")]
    public void EscapeControlChars_RendersUnnamedControlsAsMnemonics(int[] codePoints, string expected)
    {
        Assert.Equal(expected, ControlCharacters.EscapeControlChars(BuildFromCodePoints(codePoints)));
    }

    // Materializes a string from raw code points at runtime so control bytes never appear literally
    // in this source file.
    private static string BuildFromCodePoints(int[] codePoints)
    {
        var chars = new char[codePoints.Length];
        for (int i = 0; i < codePoints.Length; i++)
            chars[i] = (char)codePoints[i];
        return new string(chars);
    }
}

using System.Text;

namespace pg_protoexport;

/// <summary>
/// Shared logic for recognizing and rendering C0 control characters (0x00-0x1F) and DEL (0x7F) that
/// can appear in wire-protocol text fields when a binary-format value is misdecoded as UTF-8 (e.g. a
/// binary-format DataRow column). Exporters whose output format has its own syntax (tab-separated
/// columns, line-oriented diagram grammars, ...) must not let these bytes through raw, or they can
/// corrupt the output (a stray \t inserts a phantom column, a stray \n splits a record/line in two).
/// </summary>
public static class ControlCharacters
{
    public static bool IsControl(char c) => c < 0x20 || c == 0x7F;

    // Standard ASCII mnemonics for the C0 control block (0x00-0x1F), indexed by code point.
    // 0x7F (DEL) is handled separately in Mnemonic.
    private static readonly string[] C0Mnemonics =
    {
        "NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL",
        "BS",  "HT",  "LF",  "VT",  "FF",  "CR",  "SO",  "SI",
        "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB",
        "CAN", "EM",  "SUB", "ESC", "FS",  "GS",  "RS",  "US",
    };

    /// <summary>
    /// Maps a non-printable control character to its standard ASCII mnemonic (e.g. 0x00 → <c>NUL</c>,
    /// 0x02 → <c>STX</c>, 0x7F → <c>DEL</c>). Only valid for <see cref="IsControl"/>-true characters.
    /// </summary>
    public static string Mnemonic(char c) => c == 0x7F ? "DEL" : C0Mnemonics[c];

    /// <summary>
    /// Generic, format-neutral escape for exporters that just need a safe, single-line, human-readable
    /// substitute for control bytes (no LaTeX/JSON-specific syntax to also worry about): <c>\n</c>,
    /// <c>\r</c>, <c>\t</c> keep their familiar C-style escapes, a literal backslash is doubled so the
    /// escape marker stays unambiguous, and every other control byte (incl. NUL) renders as
    /// <c>\&lt;MNEMONIC&gt;</c> (e.g. <c>\NUL</c>, <c>\STX</c>, <c>\DEL</c>).
    /// </summary>
    public static string EscapeControlChars(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        StringBuilder? sb = null;
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            string? replacement = c switch
            {
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ when IsControl(c) => "\\" + Mnemonic(c),
                _ => null,
            };

            if (replacement is null)
            {
                sb?.Append(c);
                continue;
            }

            sb ??= new StringBuilder(raw.Length + 8).Append(raw, 0, i);
            sb.Append(replacement);
        }

        return sb?.ToString() ?? raw;
    }
}

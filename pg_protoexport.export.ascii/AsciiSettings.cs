using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class AsciiSettings : SingleFileExportSettings
{
    protected override string OutputExtension => ".txt";

    [Description("Maximum characters per output line before cells wrap to a new row. Default 160. Must be in [[40, 400]].")]
    [CommandOption("--max-width")]
    public int? MaxWidth { get; init; }

    protected override ValidationResult ValidateExtended()
    {
        if (MaxWidth is int mw && (mw < 40 || mw > 400))
            return ValidationResult.Error($"--max-width must be in [40, 400], got {mw}.");

        return ValidationResult.Success();
    }
}

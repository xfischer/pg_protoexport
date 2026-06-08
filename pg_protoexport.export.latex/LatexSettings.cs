using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class LatexSettings : SingleFileExportSettings
{
    protected override string OutputExtension => ".tex";
    protected override bool MultipleFiles => Multiple ?? false;

    [Description("When set will generate standlone LaTeX documents, ideal for short messages. Leave unset to generate LaTeX articles with page breaks when possible")]
    [CommandOption("-s|--standalone")]
    [DefaultValue(false)]
    public bool? Standalone { get; init; }

    [Description("When set, one file is generated per message in standalone mode")]
    [CommandOption("-m|--multiple")]
    [DefaultValue(false)]
    public bool? Multiple { get; init; }

    [Description("Render each bitbox with width equal to the on-the-wire byte count of the field. Long content wraps to the next bytefield row and string content is emitted as LaTeX-escaped literal UTF-8 (no truncation).")]
    [CommandOption("-x|--exact")]
    public bool? Exact { get; init; }

    [Description("Bytefield row width in bytes (used for wrapping in --exact mode). Default 32. Typical values: 16, 32, 64. Must be in [[8, 256]].")]
    [CommandOption("--row-bytes")]
    public int? RowWidthBytes { get; init; }

    protected override ValidationResult ValidateExtended()
    {
        if (RowWidthBytes is int rb && (rb < 8 || rb > 256))
            return ValidationResult.Error($"--row-bytes must be in [8, 256], got {rb}.");

        return ValidationResult.Success();
    }
}

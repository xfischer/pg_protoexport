using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace pg_protoexport;

/// <summary>
/// Branch-level settings shared by both ASCII modes (<c>fields</c> / <c>sequenceDiagram</c>).
/// Declaring the options here (rather than on the leaf <see cref="AsciiSettings"/>) makes them
/// surface in <c>ascii --help</c>, since Spectre populates a branch's OPTIONS section from the
/// branch settings type. The leaf settings inherit these, so the options remain usable after the
/// sub-command too (e.g. <c>ascii file.pcapng sequenceDiagram --console</c>).
/// </summary>
public class AsciiBranchSettings : ExportSettings
{
    [Description("Maximum characters per output line before cells wrap to a new row. Default 160. Must be in [[40, 400]].")]
    [CommandOption("--max-width")]
    public int? MaxWidth { get; init; }

    [Description("Write the output to the console (stdout) instead of a file. The output path argument becomes optional.")]
    [CommandOption("-c|--console")]
    public bool Console { get; init; }

    public override ValidationResult Validate()
    {
        var baseValidation = base.Validate();
        if (!baseValidation.Successful)
            return baseValidation;

        if (MaxWidth is int mw && (mw < 40 || mw > 400))
            return ValidationResult.Error($"--max-width must be in [40, 400], got {mw}.");

        return ValidationResult.Success();
    }
}

/// <summary>
/// Leaf settings for the ASCII sub-commands: adds the optional output-path argument and resolves it
/// from the input file (mirroring <see cref="SingleFileExportSettings"/>, which can't be reused here
/// because it derives straight from <see cref="ExportSettings"/> rather than the ascii branch type).
/// </summary>
public sealed class AsciiSettings : AsciiBranchSettings
{
    [Description("Output file path. Leave empty to generate a file at the same location as input file. Ignored with --console.")]
    [CommandArgument(0, "[output_path]")]
    public string? OutputPath { get; private set; }

    public override ValidationResult Validate()
    {
        var baseValidation = base.Validate();
        if (!baseValidation.Successful)
            return baseValidation;

        // With --console the path is unused, but resolving it is harmless (no file is created here).
        OutputPath = CheckAndFixOutputFile(InputFile, OutputPath, multiple: false, ".txt");

        return ValidationResult.Success();
    }
}

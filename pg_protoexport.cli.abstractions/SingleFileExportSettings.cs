using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace pg_protoexport;

/// <summary>
/// Base for exporters that produce a single output file. Owns the optional <c>[output_path]</c>
/// argument and the shared validation that resolves it from the input file. Derived settings only
/// declare their own options and override <see cref="OutputExtension"/> (and, if needed,
/// <see cref="MultipleFiles"/> or <see cref="ValidateExtended"/>).
/// </summary>
public abstract class SingleFileExportSettings : ExportSettings
{
    [Description("Output file path. Leave empty to generate a file at the same location as input file")]
    [CommandArgument(0, "[output_path]")]
    public string? OutputPath { get; protected set; }

    /// <summary>Default extension used when the output path is derived from the input file.</summary>
    protected abstract string OutputExtension { get; }

    /// <summary>When true, the output path resolves to a directory/stem rather than a single file.</summary>
    protected virtual bool MultipleFiles => false;

    public sealed override ValidationResult Validate()
    {
        var baseValidation = base.Validate();
        if (!baseValidation.Successful)
            return baseValidation;

        OutputPath = CheckAndFixOutputFile(InputFile, OutputPath, MultipleFiles, OutputExtension);

        return ValidateExtended();
    }

    /// <summary>Hook for exporter-specific option validation; runs after the output path is resolved.</summary>
    protected virtual ValidationResult ValidateExtended() => ValidationResult.Success();
}

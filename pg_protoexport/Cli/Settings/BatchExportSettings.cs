using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace pg_protoexport;

public class BatchExportSettings : CommandSettings
{
    [Description("Directory containing .pcapng / .pcap files to export.")]
    [CommandArgument(0, "<input_dir>")]
    public required string InputDir { get; init; }

    [Description("Output directory. One subfolder per input pcapng will be created here. Defaults to docs/examples/exports.")]
    [CommandArgument(1, "[output_dir]")]
    public string? OutputDir { get; set; }

    [Description("PostgreSQL port number. Only packets from/to this port will be processed. If omitted, auto-detected per file from the TCP SYN handshake.")]
    [CommandOption("-p|--port")]
    public ushort? Port { get; set; }

    [Description("Recurse into subdirectories of <input_dir>.")]
    [CommandOption("-r|--recursive")]
    [DefaultValue(false)]
    public bool Recursive { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(InputDir))
            return ValidationResult.Error("<input_dir> is required.");

        if (!Directory.Exists(InputDir))
            return ValidationResult.Error($"Input directory '{InputDir}' does not exist.");

        OutputDir ??= Path.Combine("docs", "examples", "exports");

        return ValidationResult.Success();
    }
}

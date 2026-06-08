using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace pg_protoexport;

public class CaptureSettings : CommandSettings
{
    [Description("Output pcapng file path")]
    [CommandArgument(0, "[output_path]")]
    public string? OutputPath { get; init; }

    [Description("PostgreSQL host (used to auto-pick the capture device). Defaults to localhost")]
    [CommandOption("--host")]
    [DefaultValue("localhost")]
    public string Host { get; set; } = "localhost";

    [Description("PostgreSQL port. Defaults to 5432")]
    [CommandOption("--port")]
    [DefaultValue((ushort)5432)]
    public ushort Port { get; set; } = 5432;

    [Description("Capture device override (Name or Description). Auto-picked from --host if omitted.")]
    [CommandOption("--device")]
    public string? Device { get; set; }

    [Description("Capture duration (e.g. 30s, 5m). If omitted, capture runs until Ctrl+C.")]
    [CommandOption("--duration")]
    public TimeSpan? Duration { get; set; }

    [Description("List available capture devices and exit.")]
    [CommandOption("--list-devices")]
    [DefaultValue(false)]
    public bool ListDevices { get; set; }

    [Description("Do not echo per-packet lines to the console while capturing.")]
    [CommandOption("--quiet")]
    [DefaultValue(false)]
    public bool Quiet { get; set; }

    public override ValidationResult Validate()
    {
        if (ListDevices) return ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(OutputPath))
            return ValidationResult.Error("output_path is required (or use --list-devices)");

        if (Duration is { } d && d <= TimeSpan.Zero)
            return ValidationResult.Error("--duration must be positive");

        return ValidationResult.Success();
    }
}

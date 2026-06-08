using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace pg_protoexport;

public class ExportSettings : CommandSettings
{
    [Description("Capture file to translate (.pcapng, .pcap)")]
    [CommandArgument(0, "<capture_file>")]
    public required string InputFile { get; init; }

    [Description("PostgreSQL port number. Only packets from/to this port will be processed. If omitted, auto-detected from the TCP SYN handshake.")]
    [CommandOption("-p|--port")]
    public ushort? Port { get; set; }

    public override ValidationResult Validate()
    {
        if (!CheckInputFile(InputFile, out var result))
            return result;

        return ValidationResult.Success();
    }

    static bool CheckInputFile(string inputFile, out ValidationResult result)
    {
        result = ValidationResult.Success();
        if (string.IsNullOrWhiteSpace(inputFile))
        {
            result = ValidationResult.Error($"Input file argument missing.");
            return false;
        }

        if (!File.Exists(inputFile))
        {
            result = ValidationResult.Error($"Input file {inputFile} does not exists.");
            return false;
        }

        List<string> supportedFileTypes = [".pcap", ".pcapng"];
        var fileExt = Path.GetExtension(inputFile).ToLowerInvariant();
        if (!supportedFileTypes.Contains(fileExt))
        {
            result = ValidationResult.Error($"Non supported input file. Supported types are {string.Join(", ", supportedFileTypes)}.");
            return false;
        }

        return true;
    }

    protected static string CheckAndFixOutputFile(string inputFile, string? outputPath, bool multiple, string fileExtension = ".txt")
    {
        inputFile = Path.GetFullPath(inputFile);
        if (outputPath == null)
            return multiple ?
                Path.Combine(Path.GetDirectoryName(inputFile)!, Path.GetFileNameWithoutExtension(inputFile))
                : Path.ChangeExtension(inputFile, fileExtension)!;

        outputPath = Path.GetFullPath(outputPath);
        bool isDirectory = string.IsNullOrEmpty(Path.GetExtension(outputPath));

        if (isDirectory)
        {
            Directory.CreateDirectory(outputPath);
            return multiple ?
                Path.Combine(outputPath, Path.GetFileNameWithoutExtension(Path.GetFileName(inputFile)))
                : Path.Combine(outputPath, Path.ChangeExtension(Path.GetFileName(inputFile), fileExtension)!);
        }

        return outputPath;
    }
}

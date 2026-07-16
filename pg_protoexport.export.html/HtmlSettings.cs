using System.ComponentModel;
using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class HtmlSettings : SingleFileExportSettings
{
    protected override string OutputExtension => ".html";

    [Description("Show a timeline annotation on the first card of every packet: an absolute timestamp on the first packet, then the elapsed time since the previous packet and since capture start.")]
    [CommandOption("--timeline")]
    public bool? Timeline { get; init; }
}

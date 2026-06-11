using System.ComponentModel;
using Spectre.Console.Cli;

namespace pg_protoexport;

public sealed class DemoSettings : CommandSettings
{
    [Description("Walk through the tour without prompting or executing anything (also what you get when output is piped / non-interactive).")]
    [CommandOption("--no-run")]
    [DefaultValue(false)]
    public bool NoRun { get; init; }
}

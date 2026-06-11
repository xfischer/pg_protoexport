using System.Runtime.InteropServices;
using Spectre.Console;
using Spectre.Console.Cli;

namespace pg_protoexport;

/// <summary>Prints the pg_protoexport version (from <see cref="VersionInfo"/>) plus runtime/OS info.</summary>
public class VersionCommand(IAnsiConsole console) : Command<EmptyCommandSettings>
{
    protected override int Execute(CommandContext context, EmptyCommandSettings settings, CancellationToken cancellation)
    {
        console.MarkupLine($"[bold]pg_protoexport[/] {Markup.Escape(VersionInfo.Informational)}");
        console.MarkupLine($"[grey]{Markup.Escape(RuntimeInformation.FrameworkDescription)} · {Markup.Escape(RuntimeInformation.OSDescription.Trim())}[/]");
        return 0;
    }
}

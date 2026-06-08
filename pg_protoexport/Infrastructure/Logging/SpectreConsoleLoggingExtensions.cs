using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace pg_protoexport.Infrastructure;

public static class SpectreConsoleLoggingExtensions
{
    public static ILoggingBuilder AddSpectreConsole(this ILoggingBuilder loggingBuilder, IAnsiConsole console)
    {
        loggingBuilder.AddProvider(new SpectreConsoleLoggerProvider(console));
        return loggingBuilder;
    }
}

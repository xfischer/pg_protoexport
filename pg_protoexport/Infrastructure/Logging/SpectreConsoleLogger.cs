using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace pg_protoexport.Infrastructure;

public class SpectreConsoleLogger(IAnsiConsole console, string name, LogLevel minLevel, bool includePrefix = true, bool includeCategory = false, bool includeEventId = false) : ILogger
{

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= minLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }
        var prefix = includePrefix
            ? GetLevelMarkup(logLevel)
            : string.Empty;
        var category = includeCategory ? name : "";
        var categoryStr = includeEventId
            ? category + $"[grey][[{eventId.Id}]][/]"
            : category;

        if (exception != null)
            console.WriteException(exception);

        console.MarkupLine(string.Concat(prefix, categoryStr, formatter(state, exception)));
    }
    private static string GetLevelMarkup(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "[italic dim grey]trce[/]: ",
            LogLevel.Debug => "[dim grey]dbug[/]: ",
            LogLevel.Information => "[dim deepskyblue2]info[/]: ",
            LogLevel.Warning => "[bold orange3]warn[/]: ",
            LogLevel.Error => "[bold red]fail[/]: ",
            LogLevel.Critical => "[bold underline red on white]crit[/]: ",
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };
    }
}

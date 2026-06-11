using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace pg_protoexport;

public static class LoggerFactoryExtensions
{
    /// <summary>
    /// Returns a logger for <typeparamref name="T"/>, or <see cref="NullLogger{T}.Instance"/> when
    /// no factory is supplied. Lets the static <c>Create()</c> factory methods (used by the no-DI
    /// consumers) skip the repeated null-check dance.
    /// </summary>
    public static ILogger<T> CreateLoggerOrNull<T>(this ILoggerFactory? loggerFactory) =>
        loggerFactory?.CreateLogger<T>() ?? NullLogger<T>.Instance;
}

using Spectre.Console.Cli;

namespace pg_protoexport.Infrastructure;

/// <summary>
/// Resolves services from the DI container for Spectre.Console commands.
/// Does not implement IDisposable — the IServiceProvider lifetime is managed by Spectre.Console's CommandApp.
/// </summary>
public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver
{
    private readonly IServiceProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return _provider.GetService(type);
    }
}

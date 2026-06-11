using System.Reflection;

namespace pg_protoexport;

/// <summary>
/// Single source of truth for the CLI's version string. Reads the
/// <see cref="AssemblyInformationalVersionAttribute"/> stamped by Nerdbank.GitVersioning at build
/// time (e.g. <c>1.1.3+bd76cda79b</c>), falling back to the assembly version if it is missing.
/// </summary>
internal static class VersionInfo
{
    public static string Informational =>
        typeof(VersionInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(VersionInfo).Assembly.GetName().Version?.ToString()
        ?? "unknown";
}

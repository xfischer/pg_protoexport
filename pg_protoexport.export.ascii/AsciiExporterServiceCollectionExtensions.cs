using Microsoft.Extensions.DependencyInjection;

namespace pg_protoexport;

public static class AsciiExporterServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ASCII-art exporter and forces the PCAP parser to record per-field metadata
    /// (offset / length / display value) since the renderer needs it to lay out cells.
    /// </summary>
    public static IServiceCollection AddAsciiExporter(
        this IServiceCollection services,
        Action<PcapToAsciiOptions>? options = null)
    {
        if (options is not null)
        {
            services.PostConfigure(options);
        }
        else
        {
            services.AddOptions<PcapToAsciiOptions>();
        }

        services.AddTransient<IPcapToAsciiService, PcapToAsciiService>();
        services.AddTransient<IPcapExporter, PcapToAsciiService>();
        services.AddSingleton<IExporterCliModule, AsciiCliModule>();
        services.PostConfigure<PcapPostgresOptions>(opt => opt.RecordFieldMetadata = true);
        return services;
    }
}

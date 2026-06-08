using Microsoft.Extensions.DependencyInjection;

namespace pg_protoexport;

public static class LatexExporterServiceCollectionExtensions
{
    public static IServiceCollection AddLatexExporter(
        this IServiceCollection services,
        Action<PcapToLatexOptions>? options = null)
    {
        if (options is not null)
        {
            services.PostConfigure(options);
        }

        services.AddTransient<IPcapToLatexService, PcapToLatexService>();
        services.AddTransient<IPcapExporter, PcapToLatexService>();
        services.AddSingleton<IExporterCliModule, LatexCliModule>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace pg_protoexport;

public static class MermaidExporterServiceCollectionExtensions
{
    public static IServiceCollection AddMermaidExporter(this IServiceCollection services)
    {
        services.AddTransient<IPcapToMermaidService, PcapToMermaidService>();
        services.AddTransient<IPcapExporter, PcapToMermaidService>();
        services.AddSingleton<IExporterCliModule, MermaidCliModule>();
        return services;
    }
}

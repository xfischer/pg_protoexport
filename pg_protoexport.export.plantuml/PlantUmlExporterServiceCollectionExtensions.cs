using Microsoft.Extensions.DependencyInjection;

namespace pg_protoexport;

public static class PlantUmlExporterServiceCollectionExtensions
{
    public static IServiceCollection AddPlantUmlExporter(this IServiceCollection services)
    {
        services.AddTransient<IPcapToPlantUmlService, PcapToPlantUmlService>();
        services.AddTransient<IPcapExporter, PcapToPlantUmlService>();
        services.AddSingleton<IExporterCliModule, PlantUmlCliModule>();
        return services;
    }
}

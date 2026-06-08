using Microsoft.Extensions.DependencyInjection;

namespace pg_protoexport;

public static class PqTraceExporterServiceCollectionExtensions
{
    public static IServiceCollection AddPqTraceExporter(
        this IServiceCollection services)
    {
        services.AddTransient<IPcapToPqTraceService, PcapToPqTraceService>();
        services.AddTransient<IPcapExporter, PcapToPqTraceService>();
        services.AddSingleton<IExporterCliModule, PqTraceCliModule>();
        return services;
    }
}

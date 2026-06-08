using Microsoft.Extensions.DependencyInjection;

namespace pg_protoexport;

public static class HtmlExporterServiceCollectionExtensions
{
    public static IServiceCollection AddHtmlExporter(this IServiceCollection services)
    {
        services.AddTransient<IPcapToHtmlService, PcapToHtmlService>();
        services.AddTransient<IPcapExporter, PcapToHtmlService>();
        services.AddSingleton<IExporterCliModule, HtmlCliModule>();
        services.PostConfigure<PcapPostgresOptions>(opt => opt.RecordFieldMetadata = true);
        return services;
    }
}

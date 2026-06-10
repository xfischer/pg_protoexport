using Microsoft.Extensions.DependencyInjection;

namespace pg_protoexport;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPcapService(
        this IServiceCollection services,
        Action<PcapPostgresOptions>? captureOptions = null)
    {
        services.Configure<PcapPostgresOptions>(opts =>
        {
            opts.AddDefaultPostgresMessages();
        });

        if (captureOptions is not null)
        {
            services.PostConfigure(captureOptions);
        }

        services.AddTransient<IPcapService, PcapService>();

        return services;
    }

    public static IServiceCollection AddLiveCapture(this IServiceCollection services)
    {
        services.AddSingleton<ILiveCaptureSessionFactory, LiveCaptureSessionFactory>();
        return services;
    }
}

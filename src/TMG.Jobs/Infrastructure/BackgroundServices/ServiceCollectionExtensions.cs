using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TMG.Jobs.Infrastructure.BackgroundServices;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundServiceReadinessTracking(this IServiceCollection services)
    {
        services.TryAddSingleton<BackgroundServiceReadinessState>();
        return services;
    }
}

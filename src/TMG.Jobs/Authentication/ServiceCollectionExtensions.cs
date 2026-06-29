using TMG.Infrastructure.Authentication;
using TMG.Jobs.Infrastructure.BackgroundServices;

namespace TMG.Jobs.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIpAddressLocationEnrichment(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration
            .GetSection(IpAddressLocationEnrichmentOptions.SectionName)
            .Get<IpAddressLocationEnrichmentOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{IpAddressLocationEnrichmentOptions.SectionName}' is required.");

        options.Validate();

        services.Configure<IpAddressLocationEnrichmentOptions>(
            configuration.GetSection(IpAddressLocationEnrichmentOptions.SectionName));
        services.AddIpGeolocationServices(configuration);
        services.AddSingleton(new BackgroundServiceDescriptor(IpAddressLocationEnrichmentProcessor.ServiceName));
        services.AddHostedService<IpAddressLocationEnrichmentProcessor>();

        return services;
    }
}

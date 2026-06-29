using TMG.Domain.Payments.Services;
using TMG.Infrastructure.Http;
using TMG.Infrastructure.Payments.Credo;
using TMG.Infrastructure.Payments.SafeHaven;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TMG.Infrastructure.Payments;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
    {
        var safeHavenOptions = configuration.GetSection(SafeHavenOptions.SectionName).Get<SafeHavenOptions>() ?? new SafeHavenOptions();
        var credoOptions = configuration.GetSection(CredoOptions.SectionName).Get<CredoOptions>() ?? new CredoOptions();

        services.Configure<SafeHavenOptions>(configuration.GetSection(SafeHavenOptions.SectionName));
        services.Configure<CredoOptions>(configuration.GetSection(CredoOptions.SectionName));

        services.AddHttpClient(PaymentHttpClientNames.SafeHaven, client =>
        {
            client.BaseAddress = new Uri(safeHavenOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(HttpRetryPolicies.CreateTransientRetryPolicy());
        services.AddHttpClient(PaymentHttpClientNames.Credo, client =>
        {
            client.BaseAddress = new Uri(credoOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(HttpRetryPolicies.CreateTransientRetryPolicy());

        services.AddScoped<ISafeHavenClient, SafeHavenClient>();
        services.AddScoped<ICredoClient, CredoClient>();
        services.AddScoped<ICredoWebhookSignatureValidator, CredoWebhookSignatureValidator>();
        services.AddScoped<IPaymentProviderService, SafeHavenPaymentProviderService>();
        services.AddScoped<IPaymentProviderService, CredoPaymentProviderService>();

        return services;
    }
}

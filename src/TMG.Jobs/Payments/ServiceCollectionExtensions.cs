using TMG.Jobs.Infrastructure.BackgroundServices;

namespace TMG.Jobs.Payments;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentReconciliation(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaymentReconciliationOptions>(configuration.GetSection(PaymentReconciliationOptions.SectionName));
        services.AddSingleton(new BackgroundServiceDescriptor(PaymentReconciliationProcessor.ServiceName));
        services.AddHostedService<PaymentReconciliationProcessor>();

        return services;
    }
}

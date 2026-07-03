using TMG.Jobs.Infrastructure.BackgroundServices;

namespace TMG.Jobs.RentReminders;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRentReminders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RentReminderOptions>(configuration.GetSection(RentReminderOptions.SectionName));
        services.AddSingleton(new BackgroundServiceDescriptor(RentReminderProcessor.ServiceName));
        services.AddHostedService<RentReminderProcessor>();

        return services;
    }
}

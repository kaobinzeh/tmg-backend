using TMG.Domain.Common.Notifications;
using TMG.Domain.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TMG.Infrastructure.Notifications;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration
            .GetSection(EmailNotificationsOptions.SectionName)
            .Get<EmailNotificationsOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{EmailNotificationsOptions.SectionName}' is required.");

        options.Validate();

        services.Configure<EmailNotificationsOptions>(configuration.GetSection(EmailNotificationsOptions.SectionName));
        services.AddScoped<IEmailNotificationService, EmailNotificationDispatcher>();
        services.AddScoped<IEmailTransportProvider, MailtrapEmailTransportProvider>();
        services.AddSingleton<INoticeLetterRenderer, NoticeLetterRenderer>();
        services.AddSingleton<IRentReceiptRenderer, RentReceiptRenderer>();

        return services;
    }

    public static IServiceCollection AddNotificationWebhookServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailNotificationsOptions>(configuration.GetSection(EmailNotificationsOptions.SectionName));
        services.AddScoped<IMailtrapWebhookSignatureValidator, MailtrapWebhookSignatureValidator>();
        // Rent receipts are rendered in-process by the RecordRentPayment handler (WebAPI host).
        services.AddSingleton<IRentReceiptRenderer, RentReceiptRenderer>();

        return services;
    }
}

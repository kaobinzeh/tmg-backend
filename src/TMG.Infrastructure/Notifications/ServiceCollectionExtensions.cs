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
        services.AddScoped<IRentReceiptArchiver, RentReceiptArchiver>();
        services.AddSingleton<ITenancyAgreementRenderer, TenancyAgreementRenderer>();
        services.AddScoped<ITenancyAgreementArchiver, TenancyAgreementArchiver>();

        return services;
    }

    public static IServiceCollection AddNotificationWebhookServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailNotificationsOptions>(configuration.GetSection(EmailNotificationsOptions.SectionName));
        services.AddScoped<IMailtrapWebhookSignatureValidator, MailtrapWebhookSignatureValidator>();
        // Receipts + agreements are rendered + archived in-process by the WebAPI-hosted handlers
        // (RecordRentPayment, AcceptTenancyInvitation).
        services.AddSingleton<IRentReceiptRenderer, RentReceiptRenderer>();
        services.AddScoped<IRentReceiptArchiver, RentReceiptArchiver>();
        services.AddSingleton<ITenancyAgreementRenderer, TenancyAgreementRenderer>();
        services.AddScoped<ITenancyAgreementArchiver, TenancyAgreementArchiver>();

        return services;
    }
}

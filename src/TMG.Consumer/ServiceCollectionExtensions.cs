using TMG.Consumer.Authentication;
using TMG.Consumer.Notifications;
using TMG.Consumer.Payments;
using TMG.Consumer.Tenancies;
using TMG.Contracts.Commands.Payments;
using TMG.Infrastructure.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer.DependencyInjection;
using ResetPassword = TMG.Contracts.Commands.Authentication.ResetPasswordCommand;
using SendNotification = TMG.Contracts.Commands.Notifications.SendNotificationCommand;
using UserAccessTokenRefreshedEvent = TMG.Contracts.Events.UserAccessTokenRefreshed;
using UserCreatedEvent = TMG.Contracts.Events.UserCreated;
using UserEmailConfirmedEvent = TMG.Contracts.Events.UserEmailConfirmed;
using SuccessfulPaymentConfirmedEvent = TMG.Contracts.Events.SuccessfulPaymentConfirmed;
using EmailDeliveryWebhookReceivedEvent = TMG.Contracts.Events.EmailDeliveryWebhookReceived;
using UserSignInFailedEvent = TMG.Contracts.Events.UserSignInFailed;
using UserSignInSuccessfulEvent = TMG.Contracts.Events.UserSignInSuccessful;
using TenantInvitedEvent = TMG.Contracts.Events.TenantInvited;
using RentDueReminderTriggeredEvent = TMG.Contracts.Events.RentDueReminderTriggered;
using RentPaymentReceivedEvent = TMG.Contracts.Events.RentPaymentReceived;

namespace TMG.Consumer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSubscribers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILoginActivityIpAddressResolver, LoginActivityIpAddressResolver>();

        var options = configuration
            .GetSection(RabbitMqMessagingOptions.SectionName)
            .Get<RabbitMqMessagingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{RabbitMqMessagingOptions.SectionName}' is required.");

        options.Validate();

        var subscriberConfig = new SubscriberConfig
        {
            ServiceName = options.ServiceName,
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            SubscriptionName = "authentication-events",
            ExchangeName = options.EventsExchange,
            PrefetchCount = 5,
            MaxRetryCount = 10,
            ConcurrentMessageCount = 1
        };

        var consumerConfig = new ConsumerConfig
        {
            ServiceName = options.ServiceName,
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            QueueName = "notifications",
            ExchangeName = options.CommandsExchange,
            PrefetchCount = 5,
            MaxRetryCount = 10,
            ConcurrentMessageCount = 1
        };

        services
            .AddSubscriber(subscriberConfig, builder => builder
                .AddHandler<UserCreatedEvent, UserCreatedHandler>()
                .AddHandler<UserEmailConfirmedEvent, UserEmailConfirmedHandler>()
                .AddHandler<UserSignInSuccessfulEvent, UserSignInSuccessfulHandler>()
                .AddHandler<UserAccessTokenRefreshedEvent, UserAccessTokenRefreshedHandler>()
                .AddHandler<UserSignInFailedEvent, UserSignInFailedHandler>()
                .AddHandler<EmailDeliveryWebhookReceivedEvent, EmailDeliveryWebhookReceivedHandler>()
                .AddHandler<SuccessfulPaymentConfirmedEvent, SuccessfulPaymentConfirmedHandler>()
                .AddHandler<TenantInvitedEvent, TenantInvitedHandler>()
                .AddHandler<RentDueReminderTriggeredEvent, RentDueReminderTriggeredHandler>()
                .AddHandler<RentPaymentReceivedEvent, RentPaymentReceivedHandler>())
            .AddConsumer(consumerConfig, builder => builder
                .AddHandler<ResetPassword, ResetPasswordHandler>()
                .AddHandler<SendNotification, SendNotificationHandler>()
                .AddHandler<CreditWalletCommand, CreditWalletHandler>()
                .AddHandler<ActivateSubscriptionCommand, ActivateSubscriptionHandler>())
            .AddHostedService(serviceProvider => new Worker(
                serviceProvider.GetRequiredKeyedService<ISubscriber>(subscriberConfig.Key),
                serviceProvider.GetRequiredKeyedService<IConsumer>(consumerConfig.Key),
                serviceProvider.GetRequiredService<ILogger<Worker>>(),
                serviceProvider.GetRequiredService<WorkerReadinessState>(),
                serviceProvider.GetRequiredService<TimeProvider>()));

        return services;
    }
}

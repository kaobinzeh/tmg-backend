using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Entities;
using TMG.Domain.Providers.Entities;
using TMG.Domain.Notifications.Specifications;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Stakeholders.Specifications;
using TMG.Infrastructure.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace TMG.Infrastructure.UnitTests;

public sealed class WhenSendingEmailNotificationWithExistingUnsentLog_Should
{
    [Fact]
    public async Task RetryDispatch()
    {
        var providerRepository = Substitute.For<IReadRepository<Provider>>();
        var templateRepository = Substitute.For<IReadRepository<EmailNotificationTemplate>>();
        var clientRepository = Substitute.For<IReadRepository<Client>>();
        var logRepository = Substitute.For<IRepository<EmailNotificationLog>>();
        var transportProvider = Substitute.For<IEmailTransportProvider>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var timeProvider = Substitute.For<TimeProvider>();
        var now = DateTimeOffset.UtcNow;
        var clientId = Guid.CreateVersion7();
        var command = new SendNotificationCommand(
            clientId,
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            NotificationMedium.Email,
            new EmailNotificationContent(
                InfrastructureTestData.Email(),
                new Dictionary<string, string>
                {
                    ["IpAddress"] = "127.0.0.1"
                }));
        var existingLog = EmailNotificationLog.Create(
            command.MessageId,
            command.ClientId,
            command.CountryId,
            command.NotificationType,
            [],
            ((EmailNotificationContent)command.NotificationContent).To,
            null,
            null,
            now);
        var templateRoot = Path.Combine(Path.GetTempPath(), $"email-templates-{Guid.CreateVersion7():N}");
        var clientTemplateDirectory = Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "default", "NotificationTypes");
        Directory.CreateDirectory(clientTemplateDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "default", "BaseTemplate.html"),
            "<html><body>{{:BodyHtml:}}</body></html>");
        await File.WriteAllTextAsync(
            Path.Combine(clientTemplateDirectory, "SignInSuccessful.html"),
            "IP Address: {{:IpAddress:}}");

        logRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationLogByMessageIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(existingLog);
        providerRepository.FirstOrDefaultAsync(Arg.Any<ActiveProviderByTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true));
        templateRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationTemplateByNotificationTypeSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailNotificationTemplate.Create(NotificationType.SignInSuccessful, "Sign-in successful notification", "Subject {{:IpAddress:}}", "SignInSuccessful.html"));
        clientRepository.FirstOrDefaultAsync(Arg.Any<ClientByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Client.Create(clientId, "Default", "default"));
        transportProvider.ProviderKey.Returns("mailtrap");
        transportProvider.SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>())
            .Returns(new EmailTransportSendResult("mailtrap-message-id"));
        hostEnvironment.ContentRootPath.Returns(templateRoot);
        timeProvider.GetUtcNow().Returns(now);

        var sut = new EmailNotificationDispatcher(
            providerRepository,
            templateRepository,
            clientRepository,
            logRepository,
            [transportProvider],
            hostEnvironment,
            Options.Create(CreateOptions()),
            unitOfWork,
            timeProvider);

        await sut.SendAsync(command, CancellationToken.None);

        await logRepository.DidNotReceive().AddAsync(Arg.Any<EmailNotificationLog>(), Arg.Any<CancellationToken>());
        await transportProvider.Received(1).SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>());
        logRepository.Received(1).Update(Arg.Is<EmailNotificationLog>(log =>
            log.SentAtUtc == now &&
            log.ProviderMessageId == "mailtrap-message-id" &&
            log.FailureReason == null));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static EmailNotificationsOptions CreateOptions() =>
        new()
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template",
            TemplateSetsRootPath = "EmailTemplates/TemplateSets"
        };
}








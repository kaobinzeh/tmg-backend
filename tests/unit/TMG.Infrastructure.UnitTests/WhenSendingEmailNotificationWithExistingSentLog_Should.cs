using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Entities;
using TMG.Domain.Providers.Entities;
using TMG.Domain.Notifications.Specifications;
using TMG.Domain.Stakeholders.Entities;
using TMG.Infrastructure.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace TMG.Infrastructure.UnitTests;

public sealed class WhenSendingEmailNotificationWithExistingSentLog_Should
{
    [Fact]
    public async Task SkipDispatch()
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
        var command = CreateCommand();
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
        existingLog.MarkSent("mailtrap-message-id", now);

        logRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationLogByMessageIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(existingLog);

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

        var result = await sut.SendAsync(command, CancellationToken.None);

        await logRepository.DidNotReceive().AddAsync(Arg.Any<EmailNotificationLog>(), Arg.Any<CancellationToken>());
        logRepository.DidNotReceive().Update(Arg.Any<EmailNotificationLog>());
        await transportProvider.DidNotReceive().SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        result.ShouldBeNull();
    }

    private static SendNotificationCommand CreateCommand() =>
        new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            NotificationMedium.Email,
            new EmailNotificationContent(
                InfrastructureTestData.Email(),
                new Dictionary<string, string>
                {
                    ["IpAddress"] = "127.0.0.1"
                }));

    private static EmailNotificationsOptions CreateOptions() =>
        new()
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template",
            TemplateSetsRootPath = "EmailTemplates/TemplateSets"
        };
}

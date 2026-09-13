using System.Text.Json;
using TMG.Consumer.IntegrationTests.Infrastructure;
using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Persistence;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Core;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TMG.Infrastructure.Persistence;
using Shouldly;

namespace TMG.Consumer.IntegrationTests.Authentication;

[Collection(nameof(ContainersCollection))]
public sealed class WhenHandlingUserCreated_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private const string Password = "P@ssw0rd123!";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ContainersFixture _fixture = fixture;
    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private Guid _clientId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _userId;

    protected override Task InitializeWorkerTestAsync() => CreatePendingUserAsync();

    protected override Task DisposeWorkerTestAsync() => DeleteAuthenticationRecordsAsync();

    [Fact]
    public async Task GenerateSignUpOtpAndQueueNotificationCommand()
    {
        await WhenPublishingUserCreated();
        await ThenTheSignUpOtpNotificationCommandIsQueued();

        async Task WhenPublishingUserCreated()
        {
            var publisherConfig = new PublisherConfig
            {
                ServiceName = "TMG.Consumer.IntegrationTests.Publisher",
                HostName = _fixture.RabbitMqHostName,
                Port = _fixture.RabbitMqPort,
                UserName = _fixture.RabbitMqUserName,
                Password = _fixture.RabbitMqPassword,
                VirtualHost = _fixture.RabbitMqVirtualHost,
                EventsExchange = "x.events.tmg.integrationtests"
            };

            await using var publisherServices = new ServiceCollection()
                .AddLogging()
                .AddPublisher(publisherConfig)
                .BuildServiceProvider();

            var publisher = publisherServices.GetRequiredKeyedService<IPublisher>(publisherConfig.Key);
            await publisher.PublishAsync(
                new UserCreated
                {
                    StakeholderId = _stakeholderId,
                    ClientId = _clientId
                },
                CancellationToken.None,
                new Dictionary<string, string>
                {
                    [KnownMetadata.CorrelationId] = Guid.CreateVersion7().ToString("N")
                });
        }

        async Task ThenTheSignUpOtpNotificationCommandIsQueued()
        {
            await WaitForConditionAsync(async () =>
            {
                using var scope = CreateDbContextScope();
                var command = await scope.DbContext.OutboxMessages
                    .Where(message =>
                        message.Kind == OutboxMessageKind.Command &&
                        message.Type == typeof(SendNotificationCommand).FullName! &&
                        message.Payload.Contains(_email))
                    .OrderByDescending(message => message.EnqueuedAtUtc)
                    .FirstOrDefaultAsync();

                return command is not null;
            });

            using var assertionScope = CreateDbContextScope();
            var outboxMessage = await assertionScope.DbContext.OutboxMessages
                .Where(message =>
                    message.Kind == OutboxMessageKind.Command &&
                    message.Type == typeof(SendNotificationCommand).FullName! &&
                    message.Payload.Contains(_email))
                .OrderByDescending(message => message.EnqueuedAtUtc)
                .FirstAsync();

            var command = JsonSerializer.Deserialize<SendNotificationCommand>(outboxMessage.Payload, SerializerOptions);

            command.ShouldNotBeNull();
            command.ClientId.ShouldBe(_clientId);
            command.CountryId.ShouldBe(_countryId);
            command.StakeholderId.ShouldBe(_stakeholderId);
            command.NotificationType.ShouldBe(NotificationType.EmailConfirmationOtp);
            command.NotificationMedium.ShouldBe(NotificationMedium.Email);
            command.NotificationContent.ShouldBeOfType<EmailNotificationContent>();

            var content = (EmailNotificationContent)command.NotificationContent;
            content.To.ShouldBe(_email);
            content.Content["FirstName"].ShouldBe(_firstName);
            content.Content["LastName"].ShouldBe(_lastName);
            content.Content["OtpCode"].ShouldNotBeNullOrWhiteSpace();
            content.Content["OtpExpiresAtUtc"].ShouldNotBeNullOrWhiteSpace();
            content.Content["Product"].ShouldBe("TMG");

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
            var twoFactorOtpService = scope.ServiceProvider.GetRequiredService<ITwoFactorOtpService>();
            var user = await repository.GetByEmailAsync(_email);
            user.ShouldNotBeNull();
            (await twoFactorOtpService.ValidateOtpAsync(
                user.Id,
                content.Content["OtpCode"],
                OtpIntent.EmailConfirmation,
                CancellationToken.None)).ShouldBeTrue();
        }
    }

    private async Task CreatePendingUserAsync()
    {
        _email = ConsumerIntegrationTestData.Email();
        _firstName = ConsumerIntegrationTestData.FirstName();
        _lastName = ConsumerIntegrationTestData.LastName();

        _stakeholderId = Guid.CreateVersion7();
        _clientId = Guid.CreateVersion7();
        _countryId = Guid.CreateVersion7();

        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = AppUser.Create(_email, _firstName, _lastName);

        var createResult = await identityService.CreateAsync(user);
        createResult.Succeeded.ShouldBeTrue();
        _userId = user.Id;

        var now = timeProvider.GetUtcNow();
        var stakeholderType = StakeholderType.Create(_clientId, "Tenant", "tenant");
        var stakeholder = Stakeholder.Create(user.Id, _clientId, _countryId, stakeholderType.Id, _firstName, _lastName);

        await dbContext.StakeholderTypes.AddAsync(stakeholderType);
        await dbContext.Stakeholders.AddAsync(stakeholder);
        await dbContext.SaveChangesAsync();

        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            return;
        }

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await repository.GetByEmailAsync(_email);

        var outboxMessages = await dbContext.OutboxMessages
            .Where(message =>
                message.Kind == OutboxMessageKind.Command &&
                message.Type == typeof(SendNotificationCommand).FullName! &&
                message.Payload.Contains(_email))
            .ToListAsync();
        if (outboxMessages.Count > 0)
        {
            dbContext.OutboxMessages.RemoveRange(outboxMessages);
        }

        var stakeholders = await dbContext.Stakeholders
            .Where(stakeholder => stakeholder.Id == _stakeholderId)
            .ToListAsync();
        if (stakeholders.Count > 0)
        {
            dbContext.Stakeholders.RemoveRange(stakeholders);
        }

        var stakeholderTypes = await dbContext.StakeholderTypes
            .Where(stakeholderType => stakeholderType.Id == _stakeholderTypeId)
            .ToListAsync();
        if (stakeholderTypes.Count > 0)
        {
            dbContext.StakeholderTypes.RemoveRange(stakeholderTypes);
        }

        if (user is null)
        {
            await dbContext.SaveChangesAsync();
            return;
        }

        repository.Remove(user);
        await unitOfWork.SaveChangesAsync();
    }
}









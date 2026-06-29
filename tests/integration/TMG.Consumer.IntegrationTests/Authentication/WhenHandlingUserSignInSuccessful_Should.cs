using System.Text.Json;
using TMG.Consumer.IntegrationTests.Infrastructure;
using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Stakeholders.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Core;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TMG.Infrastructure.Persistence;

namespace TMG.Consumer.IntegrationTests.Authentication;

[Collection(nameof(ContainersCollection))]
public sealed class WhenHandlingUserSignInSuccessful_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private const string Password = "P@ssw0rd123!";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ContainersFixture _fixture = fixture;
    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _ipAddress = string.Empty;
    private string _userAgent = string.Empty;
    private Guid _userId;
    private Guid _clientId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;

    protected override Task InitializeWorkerTestAsync() => SeedUserAndStakeholderAsync();

    protected override Task DisposeWorkerTestAsync() => DeleteSeedDataAsync();

    [Fact]
    public async Task ResetFailedCountAndQueueSignInSuccessfulNotificationCommand()
    {
        await WhenPublishingUserSignInSuccessful();
        await ThenTheFailedCountIsResetAndTheNotificationCommandIsQueued();

        async Task WhenPublishingUserSignInSuccessful()
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
                new UserSignInSuccessful(_ipAddress, _userAgent)
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

        async Task ThenTheFailedCountIsResetAndTheNotificationCommandIsQueued()
        {
            await WaitForConditionAsync(async () =>
            {
                using var scope = CreateDbContextScope();
                var user = await scope.DbContext.Users.SingleAsync(candidate => candidate.Id == _userId);
                var command = await scope.DbContext.OutboxMessages
                    .Where(message =>
                        message.Kind == OutboxMessageKind.Command &&
                        message.Type == typeof(SendNotificationCommand).FullName! &&
                        message.Payload.Contains(_email))
                    .OrderByDescending(message => message.EnqueuedAtUtc)
                    .FirstOrDefaultAsync();

                return user.AccessFailedCount == 0 && command is not null;
            });

            using var assertionScope = CreateDbContextScope();
            var user = await assertionScope.DbContext.Users.SingleAsync(candidate => candidate.Id == _userId);
            var outboxMessage = await assertionScope.DbContext.OutboxMessages
                .Where(message =>
                    message.Kind == OutboxMessageKind.Command &&
                    message.Type == typeof(SendNotificationCommand).FullName! &&
                    message.Payload.Contains(_email))
                .OrderByDescending(message => message.EnqueuedAtUtc)
                .FirstAsync();

            var command = JsonSerializer.Deserialize<SendNotificationCommand>(outboxMessage.Payload, SerializerOptions);

            user.AccessFailedCount.ShouldBe(0);
            command.ShouldNotBeNull();
            command.ClientId.ShouldBe(_clientId);
            command.CountryId.ShouldBe(_countryId);
            command.NotificationType.ShouldBe(NotificationType.SignInSuccessful);
            command.NotificationMedium.ShouldBe(NotificationMedium.Email);
            command.NotificationContent.ShouldBeOfType<EmailNotificationContent>();
            ((EmailNotificationContent)command.NotificationContent).To.ShouldBe(_email);
        }
    }

    private async Task SeedUserAndStakeholderAsync()
    {
        _email = ConsumerIntegrationTestData.Email();
        _firstName = ConsumerIntegrationTestData.FirstName();
        _lastName = ConsumerIntegrationTestData.LastName();
        _ipAddress = ConsumerIntegrationTestData.IpAddress();
        _userAgent = ConsumerIntegrationTestData.UserAgent();
        _clientId = Guid.CreateVersion7();
        _countryId = Guid.CreateVersion7();

        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = AppUser.Create(_email, _firstName, _lastName);

        var createResult = await identityService.CreateAsync(user);
        createResult.Succeeded.ShouldBeTrue();

        user.AccessFailedCount = 3;
        user.LockoutEnabled = true;

        var updateResult = await identityService.UpdateAsync(user);
        updateResult.Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(_clientId, "Tenant", "tenant");
        var stakeholder = Stakeholder.Create(user.Id, _clientId, _countryId, stakeholderType.Id, _firstName, _lastName);

        await dbContext.StakeholderTypes.AddAsync(stakeholderType);
        await dbContext.Stakeholders.AddAsync(stakeholder);
        await dbContext.SaveChangesAsync();

        _userId = user.Id;
        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateDbContextScope();
        var dbContext = scope.DbContext;

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

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == _userId);
        if (user is not null)
        {
            dbContext.Users.Remove(user);
        }

        await dbContext.SaveChangesAsync();
    }
}









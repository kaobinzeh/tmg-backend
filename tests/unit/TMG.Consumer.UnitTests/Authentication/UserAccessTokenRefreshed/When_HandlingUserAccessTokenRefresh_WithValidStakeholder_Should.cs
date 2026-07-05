using TMG.Consumer.Authentication;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Services;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;
using Shouldly;

namespace TMG.Consumer.UnitTests.Authentication;

public sealed class When_HandlingUserAccessTokenRefresh_WithValidStakeholder_Should
{
    [Fact]
    public async Task PersistLoginActivity()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var loginActivityIpAddressResolver = Substitute.For<ILoginActivityIpAddressResolver>();
        var loginActivityRepository = Substitute.For<IRepository<LoginActivity>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userAgentParserService = Substitute.For<IUserAgentParserService>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 21, 12, 0, 0, TimeSpan.Zero));
        var stakeholderId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var message = new UserAccessTokenRefreshed(ConsumerTestData.IpAddress(), ConsumerTestData.UserAgent())
        {
            StakeholderId = stakeholderId,
            ClientId = clientId
        };

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                stakeholderId,
                Guid.CreateVersion7(),
                ConsumerTestData.Email(),
                clientId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "manager",
                ConsumerTestData.FirstName(),
                ConsumerTestData.LastName(),
                null,
                true));
        loginActivityIpAddressResolver.ResolveAsync(message.IpAddress, Arg.Any<CancellationToken>())
            .Returns(new LoginActivityIpAddressResolution(Guid.CreateVersion7(), Guid.CreateVersion7()));
        userAgentParserService.Parse(message.UserAgent)
            .Returns(new UserAgentInfo("Phone", "Android", "Chrome"));

        var sut = new UserAccessTokenRefreshedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            stakeholderReadModelRepository,
            loginActivityIpAddressResolver,
            loginActivityRepository,
            unitOfWork,
            userAgentParserService,
            timeProvider);

        await sut.HandleAsync(message, CancellationToken.None);

        await loginActivityRepository.Received(1).AddAsync(
            Arg.Is<LoginActivity>(activity =>
                activity.StakeholderId == stakeholderId &&
                activity.ClientId == clientId &&
                activity.ActivityType == LoginActivityType.TokenRefresh),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

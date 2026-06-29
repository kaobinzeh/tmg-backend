using TMG.Consumer.Authentication;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Services;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using NSubstitute;
using Shouldly;

namespace TMG.Consumer.UnitTests.Authentication;

public sealed class When_HandlingUserAccessTokenRefresh_ForUnknownStakeholder_Should
{
    [Fact]
    public async Task ThrowNonTransientException()
    {
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var stakeholderId = Guid.CreateVersion7();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns((StakeholderReadModel?)null);

        var sut = new UserAccessTokenRefreshedHandler(
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            stakeholderRepository,
            Substitute.For<ILoginActivityIpAddressResolver>(),
            Substitute.For<IRepository<LoginActivity>>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IUserAgentParserService>(),
            TimeProvider.System);

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            sut.HandleAsync(
                new UserAccessTokenRefreshed(ConsumerTestData.IpAddress(), ConsumerTestData.UserAgent())
                {
                    StakeholderId = stakeholderId,
                    ClientId = Guid.CreateVersion7()
                },
                CancellationToken.None));

        exception.Message.ShouldContain("no stakeholder could be found", Case.Insensitive);
    }
}

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

public sealed class When_HandlingUserAccessTokenRefresh_WithoutStakeholderId_Should
{
    [Fact]
    public async Task ThrowNonTransientException()
    {
        var messageContext = Substitute.For<IMessageContext>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));

        var sut = new UserAccessTokenRefreshedHandler(
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            Substitute.For<IStakeholderReadModelRepository>(),
            Substitute.For<ILoginActivityIpAddressResolver>(),
            Substitute.For<IRepository<LoginActivity>>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IUserAgentParserService>(),
            TimeProvider.System);

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            sut.HandleAsync(
                new UserAccessTokenRefreshed(ConsumerTestData.IpAddress(), ConsumerTestData.UserAgent())
                {
                    ClientId = Guid.CreateVersion7()
                },
                CancellationToken.None));

        exception.Message.ShouldContain("stakeholder actor id", Case.Insensitive);
    }
}

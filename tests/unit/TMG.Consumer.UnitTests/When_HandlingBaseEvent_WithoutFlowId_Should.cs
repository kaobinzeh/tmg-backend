using TMG.Consumer;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;

namespace TMG.Consumer.UnitTests;

public sealed class When_HandlingBaseEvent_WithoutFlowId_Should
{
    [Fact]
    public async Task SetEmptyFlowId()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var clientId = Guid.CreateVersion7();
        var correlationId = Guid.CreateVersion7().ToString("N");
        messageContext.CorrelationId.Returns(correlationId);

        await new UserCreatedHandlerForTests(customTelemetryContext, currentActorAccessor, messageContext)
            .HandleAsync(
                new UserCreated
                {
                    ClientId = clientId,
                    FlowId = null
                },
                CancellationToken.None);

        currentActorAccessor.Received(1).Set(
            Arg.Any<string>(),
            clientId,
            correlationId,
            string.Empty);
    }

    private sealed class UserCreatedHandlerForTests(
        ICustomTelemetryContext customTelemetryContext,
        ICurrentActorAccessor currentActorAccessor,
        IMessageContext messageContext)
        : BaseMessageHandler<UserCreated>(customTelemetryContext, currentActorAccessor, messageContext)
    {
        protected override Task HandleAsyncInternal(UserCreated message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

using TMG.Consumer;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using NSubstitute;
using Shouldly;

namespace TMG.Consumer.UnitTests;

public sealed class When_HandlingMessage_WithoutBaseContract_Should
{
    [Fact]
    public async Task ThrowCannotProcessMessageNonTransientException()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            new TestMessageHandler(customTelemetryContext, currentActorAccessor, messageContext)
                .HandleAsync(new TestMessage(), CancellationToken.None));

        exception.Message.ShouldBe("TestMessage must inherit from BaseCommand or BaseEvent.");
    }

    private sealed record TestMessage;

    private sealed class TestMessageHandler(
        ICustomTelemetryContext customTelemetryContext,
        ICurrentActorAccessor currentActorAccessor,
        IMessageContext messageContext)
        : BaseMessageHandler<TestMessage>(customTelemetryContext, currentActorAccessor, messageContext)
    {
        protected override Task HandleAsyncInternal(TestMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

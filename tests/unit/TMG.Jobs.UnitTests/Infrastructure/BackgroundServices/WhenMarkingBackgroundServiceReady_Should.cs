using TMG.Jobs.Infrastructure.BackgroundServices;
using Shouldly;

namespace TMG.Jobs.UnitTests.Infrastructure.BackgroundServices;

public sealed class WhenMarkingBackgroundServiceReady_Should
{
    [Fact]
    public void UpdateReadinessState()
    {
        var state = new BackgroundServiceReadinessState(
            [new BackgroundServiceDescriptor("OutboxMessageProcessor")]);

        state.MarkReady("OutboxMessageProcessor");

        state.IsReady.ShouldBeTrue();
    }
}


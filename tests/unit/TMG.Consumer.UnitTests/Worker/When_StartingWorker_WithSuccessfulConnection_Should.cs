using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace TMG.Consumer.UnitTests.Worker;

public sealed class When_StartingWorker_WithSuccessfulConnection_Should
{
    [Fact]
    public async Task MarkReady()
    {
        var subscriber = Substitute.For<ISubscriber>();
        var consumer = Substitute.For<IConsumer>();
        var logger = Substitute.For<ILogger<global::TMG.Consumer.Worker>>();
        var readinessState = new WorkerReadinessState();
        var timeProvider = TimeProvider.System;
        var cts = new CancellationTokenSource();

        var worker = new global::TMG.Consumer.Worker(
            subscriber, consumer, logger, readinessState, timeProvider);

        await worker.StartAsync(cts.Token);
        await WaitForConditionAsync(() => readinessState.IsReady);

        readinessState.IsReady.ShouldBeTrue();
        await subscriber.Received(1).StartAsync(Arg.Any<CancellationToken>());
        await consumer.Received(1).StartAsync(Arg.Any<CancellationToken>());

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new InvalidOperationException("Condition not met in time.");
    }
}

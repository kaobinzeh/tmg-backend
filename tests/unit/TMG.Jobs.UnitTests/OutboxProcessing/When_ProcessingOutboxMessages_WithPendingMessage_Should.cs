using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Persistence;
using TMG.Jobs.Infrastructure.BackgroundServices;
using TMG.Jobs.OutboxProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace TMG.Jobs.UnitTests.OutboxProcessing;

public sealed class When_ProcessingOutboxMessages_WithPendingMessage_Should
{
    [Fact]
    public async Task MarkMessageAsSent()
    {
        var repository = Substitute.For<IRepository<OutboxMessage>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dispatcher = Substitute.For<IOutboxMessageDispatcher>();
        var signal = new OutboxProcessingSignal();
        var state = new BackgroundServiceReadinessState([new BackgroundServiceDescriptor(OutboxMessageProcessor.ServiceName)]);
        var message = OutboxMessage.CreateEvent(Guid.CreateVersion7(), "type", "{}", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var services = new ServiceCollection()
            .AddSingleton(repository)
            .AddSingleton(unitOfWork)
            .AddSingleton(dispatcher)
            .BuildServiceProvider();

        repository.ListAsync(Arg.Any<ISpecification<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns([message]);
        repository.FirstOrDefaultAsync(Arg.Any<ISpecification<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns((OutboxMessage?)null);

        var sut = new OutboxMessageProcessor(
            NullLogger<OutboxMessageProcessor>.Instance,
            services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new OutboxProcessingOptions { BatchSize = 10, PollIntervalSeconds = 1 }),
            signal,
            state);

        await sut.StartAsync(CancellationToken.None);
        await WaitForConditionAsync(() => message.SentAtUtc is not null);
        await sut.StopAsync(CancellationToken.None);

        state.IsReady.ShouldBeTrue();
        message.AvailableAtUtc.ShouldNotBe(default);
        message.AvailableAtUtc.ShouldBeLessThanOrEqualTo(message.SentAtUtc!.Value);
        message.SentAtUtc.ShouldNotBeNull();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

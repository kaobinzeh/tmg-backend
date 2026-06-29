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

public sealed class When_ProcessingOutboxMessages_WhenListAsyncThrows_Should
{
    [Fact]
    public async Task ContinueRunning()
    {
        var repository = Substitute.For<IRepository<OutboxMessage>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dispatcher = Substitute.For<IOutboxMessageDispatcher>();
        var signal = new OutboxProcessingSignal();
        var state = new BackgroundServiceReadinessState([new BackgroundServiceDescriptor(OutboxMessageProcessor.ServiceName)]);
        var services = new ServiceCollection()
            .AddSingleton(repository)
            .AddSingleton(unitOfWork)
            .AddSingleton(dispatcher)
            .BuildServiceProvider();

        repository.ListAsync(Arg.Any<ISpecification<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<OutboxMessage>>(new InvalidOperationException("Database unavailable")));

        var sut = new OutboxMessageProcessor(
            NullLogger<OutboxMessageProcessor>.Instance,
            services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new OutboxProcessingOptions { BatchSize = 10, PollIntervalSeconds = 1 }),
            signal,
            state);

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(1));
        await sut.StopAsync(CancellationToken.None);

        state.IsReady.ShouldBeTrue();
        await repository.Received().ListAsync(Arg.Any<ISpecification<OutboxMessage>>(), Arg.Any<CancellationToken>());
    }
}

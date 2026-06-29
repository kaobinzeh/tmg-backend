using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Services;
using TMG.Domain.Common.Persistence;
using TMG.Jobs.Authentication;
using TMG.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace TMG.Jobs.UnitTests.Authentication.IpAddressLocationEnrichment;

public sealed class When_EnrichingIpAddresses_WhenResolverThrows_Should
{
    [Fact]
    public async Task RecordLookupTimestamp()
    {
        var repository = Substitute.For<IRepository<IpAddress>>();
        var geolocationService = Substitute.For<IIpGeolocationService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var state = new BackgroundServiceReadinessState([new BackgroundServiceDescriptor(IpAddressLocationEnrichmentProcessor.ServiceName)]);
        var ipAddress = IpAddress.Create("127.0.0.1");
        var services = new ServiceCollection()
            .AddSingleton(repository)
            .AddSingleton(geolocationService)
            .AddSingleton(unitOfWork)
            .BuildServiceProvider();

        repository.ListAsync(Arg.Any<ISpecification<IpAddress>>(), Arg.Any<CancellationToken>())
            .Returns([ipAddress]);
        geolocationService.GetGeolocationAsync(ipAddress.Value, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IpGeolocation?>(new InvalidOperationException("boom")));

        var sut = new IpAddressLocationEnrichmentProcessor(
            services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new IpAddressLocationEnrichmentOptions { BatchSize = 10, PollIntervalSeconds = 1, LocationRefreshIntervalDays = 30 }),
            state,
            NullLogger<IpAddressLocationEnrichmentProcessor>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await WaitForConditionAsync(() => ipAddress.LocationLookupTimestampUtc is not null);
        await sut.StopAsync(CancellationToken.None);

        ipAddress.LocationLookupTimestampUtc.ShouldNotBeNull();
        ipAddress.GetCurrentLocation().ShouldBeNull();
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

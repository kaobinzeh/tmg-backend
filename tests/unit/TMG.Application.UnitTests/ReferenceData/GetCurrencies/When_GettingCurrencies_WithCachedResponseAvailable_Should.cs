using TMG.Application.ReferenceData.Features.GetCurrencies;
using TMG.Domain.Common.Caching;
using TMG.Domain.Payments.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.ReferenceData.GetCurrencies;

public sealed class When_GettingCurrencies_WithCachedResponseAvailable_Should
{
    [Fact]
    public async Task ReturnCachedCurrencies()
    {
        var currencies = Substitute.For<IRepository<Currency>>();
        var cache = Substitute.For<IJsonCache>();
        var cachedResponse = new[]
        {
            new GetCurrenciesResponse(Guid.CreateVersion7(), "NGN", "Nigerian Naira", "₦")
        };

        cache.GetAsync<GetCurrenciesResponse[]>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cachedResponse);

        var sut = new GetCurrenciesHandler(currencies, cache);

        var result = await sut.HandleAsync(CancellationToken.None);

        result.ShouldBe(cachedResponse);
        await currencies.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
    }
}

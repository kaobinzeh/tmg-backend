using TMG.Application.ReferenceData.Features.GetCurrencies;
using TMG.Domain.Common.Caching;
using TMG.Domain.Payments.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.ReferenceData.GetCurrencies;

public sealed class When_GettingCurrencies_WithoutCachedResponse_Should
{
    [Fact]
    public async Task CacheAndReturnMappedCurrencies()
    {
        var currencies = Substitute.For<IRepository<Currency>>();
        var cache = Substitute.For<IJsonCache>();
        var entities = new[]
        {
            Currency.Create("NGN", "Nigerian Naira", true, "₦")
        };

        cache.GetAsync<GetCurrenciesResponse[]>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GetCurrenciesResponse[]?)null);
        currencies.ListAsync(Arg.Any<ISpecification<Currency>>(), Arg.Any<CancellationToken>())
            .Returns(entities);

        var sut = new GetCurrenciesHandler(currencies, cache);

        var result = await sut.HandleAsync(CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(entities[0].Id);
        result[0].Code.ShouldBe("NGN");
        result[0].Name.ShouldBe("Nigerian Naira");
        result[0].Symbol.ShouldBe("₦");
        await cache.Received(1).SetAsync(
            "reference-data:currencies",
            Arg.Any<GetCurrenciesResponse[]>(),
            TimeSpan.FromHours(12),
            Arg.Any<CancellationToken>());
    }
}

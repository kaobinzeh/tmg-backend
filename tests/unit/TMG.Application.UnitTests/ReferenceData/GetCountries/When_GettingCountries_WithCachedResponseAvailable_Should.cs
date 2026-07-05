using TMG.Application.ReferenceData.Features.GetCountries;
using TMG.Domain.Common.Caching;
using TMG.Domain.Common.Persistence;
using TMG.Domain.ReferenceData.Entities;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests.ReferenceData.GetCountries;

public sealed class When_GettingCountries_WithCachedResponseAvailable_Should
{
    [Fact]
    public async Task ReturnCachedCountries()
    {
        var countries = Substitute.For<IRepository<Country>>();
        var cache = Substitute.For<IJsonCache>();
        var cachedResponse = new[]
        {
            new GetCountriesResponse(Guid.CreateVersion7(), "Nigeria", "NG", "+234", "https://example.com/ng.svg")
        };

        cache.GetAsync<GetCountriesResponse[]>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cachedResponse);

        var sut = new GetCountriesHandler(countries, cache);

        var result = await sut.HandleAsync(CancellationToken.None);

        result.ShouldBe(cachedResponse);
        await countries.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
    }
}

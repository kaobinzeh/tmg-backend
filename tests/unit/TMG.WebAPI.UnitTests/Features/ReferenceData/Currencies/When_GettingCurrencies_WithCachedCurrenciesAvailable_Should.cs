using TMG.Application.ReferenceData.Features.GetCurrencies;
using TMG.Domain.Common.Caching;
using TMG.Domain.Payments.Entities;
using TMG.WebAPI.Features.ReferenceData.Currencies;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.ReferenceData.Currencies;

public sealed class When_GettingCurrencies_WithCachedCurrenciesAvailable_Should
{
    [Fact]
    public async Task ReturnCurrencies()
    {
        var repository = Substitute.For<IRepository<Currency>>();
        var cache = Substitute.For<IJsonCache>();
        var response = new[]
        {
            new GetCurrenciesResponse(Guid.CreateVersion7(), "NGN", "Nigerian Naira", "₦")
        };

        cache.GetAsync<GetCurrenciesResponse[]>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var sut = new CurrenciesController(new GetCurrenciesHandler(repository, cache));

        var result = await sut.Handle(CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(response);
    }
}

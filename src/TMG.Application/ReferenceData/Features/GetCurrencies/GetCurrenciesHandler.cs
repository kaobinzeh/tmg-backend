using TMG.Domain.Common.Caching;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;
using TMG.Domain.Payments.Specifications;

namespace TMG.Application.ReferenceData.Features.GetCurrencies;

public sealed class GetCurrenciesHandler(IRepository<Currency> currencies, IJsonCache cache)
{
    private const string CacheKey = "reference-data:currencies";

    public async Task<IReadOnlyList<GetCurrenciesResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<GetCurrenciesResponse[]>(CacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var response = (await currencies.ListAsync(new ActiveCurrenciesSpecification(), cancellationToken))
            .Select(currency => new GetCurrenciesResponse(
                currency.Id,
                currency.CurrencyCode,
                currency.CurrencyName,
                currency.CurrencySymbol))
            .ToArray();

        await cache.SetAsync(CacheKey, response, TimeSpan.FromHours(12), cancellationToken);

        return response;
    }
}

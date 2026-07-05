using TMG.Application.ReferenceData.Specifications;
using TMG.Domain.Common.Caching;
using TMG.Domain.Common.Persistence;
using TMG.Domain.ReferenceData.Entities;

namespace TMG.Application.ReferenceData.Features.GetCountries;

public sealed class GetCountriesHandler(IRepository<Country> countries, IJsonCache cache)
{
    // Versioned so cached payloads from the previous response shape are not deserialized.
    private const string CacheKey = "reference-data:countries:v2";

    public async Task<IReadOnlyList<GetCountriesResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<GetCountriesResponse[]>(CacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var response = (await countries.ListAsync(new EnabledCountriesSpecification(), cancellationToken))
            .Select(country => new GetCountriesResponse(
                country.Id,
                country.Name,
                country.ShortCode,
                country.CallingCode,
                country.FlagUrl))
            .ToArray();

        await cache.SetAsync(CacheKey, response, TimeSpan.FromHours(12), cancellationToken);

        return response;
    }
}

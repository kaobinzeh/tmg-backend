namespace TMG.Application.ReferenceData.Features.GetCountries;

public sealed record GetCountriesResponse(
    string Name,
    string ShortCode,
    string? CallingCode,
    string FlagUrl);

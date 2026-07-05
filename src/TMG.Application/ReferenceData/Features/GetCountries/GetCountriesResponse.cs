namespace TMG.Application.ReferenceData.Features.GetCountries;

public sealed record GetCountriesResponse(
    Guid Id,
    string Name,
    string ShortCode,
    string? CallingCode,
    string FlagUrl);

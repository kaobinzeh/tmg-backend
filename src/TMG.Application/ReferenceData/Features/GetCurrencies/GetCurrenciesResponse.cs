namespace TMG.Application.ReferenceData.Features.GetCurrencies;

public sealed record GetCurrenciesResponse(
    Guid Id,
    string Code,
    string Name,
    string? Symbol);

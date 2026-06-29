namespace TMG.WebAPI.Features.Properties;

public sealed record AddUnitRequest(
    string Label,
    string? Description,
    int NumberOfRooms,
    decimal RentAmount,
    Guid CurrencyId);

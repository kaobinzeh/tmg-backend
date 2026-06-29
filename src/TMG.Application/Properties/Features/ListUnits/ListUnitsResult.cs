using TMG.Domain.Properties.Entities;

namespace TMG.Application.Properties.Features.ListUnits;

public sealed record ListUnitsResult(
    ListUnitsStatus Status,
    IReadOnlyList<UnitListItem> Units);

public sealed record UnitListItem(
    Guid Id,
    string Label,
    string? Description,
    int NumberOfRooms,
    decimal RentAmount,
    Guid CurrencyId,
    UnitStatus Status);

public enum ListUnitsStatus
{
    Success = 1,
    NotAuthenticated = 2,
    PropertyNotFound = 3
}

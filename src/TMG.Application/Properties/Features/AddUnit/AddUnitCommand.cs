using TMG.Domain.Common.Auditing;

namespace TMG.Application.Properties.Features.AddUnit;

public sealed record AddUnitCommand(
    Guid PropertyId,
    string Label,
    string? Description,
    int NumberOfRooms,
    decimal RentAmount,
    Guid CurrencyId,
    ActorContext ActorContext);

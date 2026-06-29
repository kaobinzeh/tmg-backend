using TMG.Domain.Common.Auditing;

namespace TMG.Application.Properties.Features.UpdateUnitRent;

public sealed record UpdateUnitRentCommand(
    Guid UnitId,
    decimal RentAmount,
    ActorContext ActorContext);

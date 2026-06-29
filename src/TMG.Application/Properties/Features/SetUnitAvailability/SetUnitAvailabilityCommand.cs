using TMG.Domain.Common.Auditing;
using TMG.Domain.Properties.Entities;

namespace TMG.Application.Properties.Features.SetUnitAvailability;

public sealed record SetUnitAvailabilityCommand(
    Guid UnitId,
    UnitStatus Status,
    ActorContext ActorContext);

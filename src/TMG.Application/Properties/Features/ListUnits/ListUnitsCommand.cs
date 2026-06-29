using TMG.Domain.Common.Auditing;

namespace TMG.Application.Properties.Features.ListUnits;

public sealed record ListUnitsCommand(
    Guid PropertyId,
    ActorContext ActorContext);

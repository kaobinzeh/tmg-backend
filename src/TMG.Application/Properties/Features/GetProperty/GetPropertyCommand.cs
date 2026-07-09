using TMG.Domain.Common.Auditing;

namespace TMG.Application.Properties.Features.GetProperty;

public sealed record GetPropertyCommand(Guid PropertyId, ActorContext ActorContext);

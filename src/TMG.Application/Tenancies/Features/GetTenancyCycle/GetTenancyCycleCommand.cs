using TMG.Domain.Common.Auditing;

namespace TMG.Application.Tenancies.Features.GetTenancyCycle;

public sealed record GetTenancyCycleCommand(Guid TenancyId, ActorContext ActorContext);

using TMG.Domain.Common.Auditing;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Application.Tenancies.Features.ListTenancyAllocations;

public sealed record ListTenancyAllocationsCommand(ActorContext ActorContext, TenancyStatus? Status = null);

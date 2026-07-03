using TMG.Domain.Common.Auditing;

namespace TMG.Application.Tenancies.Features.ActivateTenancy;

public sealed record ActivateTenancyCommand(
    Guid TenancyId,
    ActorContext ActorContext,
    DateTimeOffset? LeaseStartDate = null,
    int? TermMonths = null);

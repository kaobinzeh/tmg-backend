using TMG.Domain.Common.Auditing;

namespace TMG.Application.Tenancies.Features.AllocateUnit;

public sealed record AllocateUnitCommand(
    Guid UnitId,
    string TenantEmail,
    string TenantFirstName,
    string TenantLastName,
    ActorContext ActorContext,
    DateTimeOffset? LeaseStartDate = null,
    int? TermMonths = null);

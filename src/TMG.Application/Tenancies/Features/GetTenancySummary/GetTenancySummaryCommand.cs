using TMG.Domain.Common.Auditing;

namespace TMG.Application.Tenancies.Features.GetTenancySummary;

public sealed record GetTenancySummaryCommand(ActorContext ActorContext, int WithinMonths = 6);

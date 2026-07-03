using TMG.Domain.Common.Auditing;

namespace TMG.Application.Tenancies.Features.ListUpcomingRenewals;

public sealed record ListUpcomingRenewalsCommand(ActorContext ActorContext, int WithinMonths = 6);

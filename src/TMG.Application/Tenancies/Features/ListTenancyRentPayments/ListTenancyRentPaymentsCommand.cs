using TMG.Domain.Common.Auditing;

namespace TMG.Application.Tenancies.Features.ListTenancyRentPayments;

public sealed record ListTenancyRentPaymentsCommand(Guid TenancyId, ActorContext ActorContext);

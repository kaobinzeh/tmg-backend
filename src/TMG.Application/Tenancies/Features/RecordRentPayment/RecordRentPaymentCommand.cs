using TMG.Domain.Common.Auditing;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Application.Tenancies.Features.RecordRentPayment;

public sealed record RecordRentPaymentCommand(
    Guid TenancyId,
    decimal Amount,
    RentPaymentMethod Method,
    ActorContext ActorContext,
    string? Reference = null,
    DateTimeOffset? PaidAtUtc = null);

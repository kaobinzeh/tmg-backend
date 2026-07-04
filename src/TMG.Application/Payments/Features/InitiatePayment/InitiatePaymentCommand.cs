using TMG.Contracts.Payments;

namespace TMG.Application.Payments.Features.InitiatePayment;

using TMG.Domain.Common.Auditing;

public sealed record InitiatePaymentCommand(
    decimal Amount,
    Guid CurrencyId,
    PaymentIntent PaymentIntent,
    Guid PaymentProviderId,
    ActorContext ActorContext,
    Guid? TenancyId = null);

using TMG.Contracts.Payments;

namespace TMG.Application.Payments.Features.GetPaymentProviders;

public sealed record GetPaymentProvidersCommand(PaymentIntent Intent);

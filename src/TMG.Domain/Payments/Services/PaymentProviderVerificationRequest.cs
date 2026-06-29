using TMG.Contracts.Payments;

namespace TMG.Domain.Payments.Services;

public sealed record PaymentProviderVerificationRequest(
    string MerchantReference,
    string? ProviderReference,
    decimal Amount,
    string CurrencyCode,
    PaymentIntent PaymentIntent);

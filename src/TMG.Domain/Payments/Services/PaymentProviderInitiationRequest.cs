using TMG.Contracts.Payments;

namespace TMG.Domain.Payments.Services;

public sealed record PaymentProviderInitiationRequest(
    string MerchantReference,
    decimal Amount,
    string CurrencyCode,
    PaymentIntent PaymentIntent,
    Guid StakeholderId,
    Guid ClientId,
    Guid CountryId);

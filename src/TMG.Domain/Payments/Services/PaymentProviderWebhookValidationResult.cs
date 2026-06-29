using TMG.Contracts.Payments;

namespace TMG.Domain.Payments.Services;

public sealed record PaymentProviderWebhookValidationResult(
    SignatureValidationStatus SignatureValidationStatus,
    string? StatusChangeReason);

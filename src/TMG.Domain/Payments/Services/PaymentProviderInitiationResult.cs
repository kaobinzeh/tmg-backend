using TMG.Contracts.Payments;

namespace TMG.Domain.Payments.Services;

public sealed record PaymentProviderInitiationResult(
    string ProviderReference,
    string PaymentProvider,
    PaymentMethodType PaymentMethodType,
    DateTimeOffset? ExpiresAtUtc,
    IReadOnlyDictionary<string, string> InstructionFields);

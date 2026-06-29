namespace TMG.Application.Payments.Features.ActivatePaymentProvider;

public sealed record ActivatePaymentProviderResult(ActivatePaymentProviderStatus Status, string? Error = null);

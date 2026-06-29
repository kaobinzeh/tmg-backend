namespace TMG.Application.Payments.Features.ActivatePaymentProvider;

public sealed record ActivatePaymentProviderCommand(Guid PaymentProviderId, bool IsActive);

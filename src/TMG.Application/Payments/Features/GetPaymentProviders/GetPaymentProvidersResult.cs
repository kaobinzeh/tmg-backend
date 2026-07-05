namespace TMG.Application.Payments.Features.GetPaymentProviders;

public sealed record GetPaymentProvidersResult(IReadOnlyList<PaymentProviderListItem> Providers);

public sealed record PaymentProviderListItem(
    Guid Id,
    string Name,
    string PaymentMethodType,
    Guid CurrencyId);

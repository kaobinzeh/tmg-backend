using TMG.Contracts.Payments;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class EnabledPaymentProviderConfigurationSpecification : Specification<PaymentProviderConfiguration>
{
    public EnabledPaymentProviderConfigurationSpecification(Guid paymentProviderId, Guid currencyId, PaymentIntent paymentIntent)
    {
        Where(configuration =>
            configuration.PaymentProviderId == paymentProviderId &&
            configuration.CurrencyId == currencyId &&
            configuration.PaymentIntent == paymentIntent &&
            configuration.IsEnabled);
    }
}

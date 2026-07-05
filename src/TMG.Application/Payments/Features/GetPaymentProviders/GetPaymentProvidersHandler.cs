using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;
using TMG.Domain.Payments.Specifications;

namespace TMG.Application.Payments.Features.GetPaymentProviders;

public sealed class GetPaymentProvidersHandler(IRepository<PaymentProvider> paymentProviderRepository)
{
    public async Task<GetPaymentProvidersResult> HandleAsync(
        GetPaymentProvidersCommand command,
        CancellationToken cancellationToken)
    {
        var providers = await paymentProviderRepository.ListAsync(
            new ActivePaymentProvidersSpecification(),
            cancellationToken);

        var items = providers
            .SelectMany(provider => provider.Configurations
                .Where(configuration => configuration.IsEnabled && configuration.PaymentIntent == command.Intent)
                .Select(configuration => new PaymentProviderListItem(
                    provider.Id,
                    provider.ProviderName,
                    configuration.PaymentMethodType.ToString(),
                    configuration.CurrencyId)))
            .ToList();

        return new GetPaymentProvidersResult(items);
    }
}

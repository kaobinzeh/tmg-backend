using TMG.Contracts.Payments;
using TMG.Domain.Common.Exceptions;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments;
using TMG.Domain.Payments.Entities;
using TMG.Domain.Payments.Services;
using TMG.Domain.Payments.Specifications;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Payments.Features.InitiatePayment;

public sealed class InitiatePaymentHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<Currency> currencyRepository,
    IRepository<CountryCurrency> countryCurrencyRepository,
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentProviderConfiguration> paymentProviderConfigurationRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IRepository<Tenancy> tenancyRepository,
    IRepository<Unit> unitRepository,
    IEnumerable<IPaymentProviderService> paymentProviderServices,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<InitiatePaymentResult> HandleAsync(InitiatePaymentCommand command, CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required to initiate a payment.");

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken)
            ?? throw new InvalidOperationException($"Unable to resolve stakeholder '{stakeholderId}' for payment initiation.");

        // Rent payments are server-authoritative: the amount and currency come from the unit, and the tenancy
        // must be active and belong to the authenticated tenant. Any client-sent amount/currency is ignored.
        var amount = command.Amount;
        var currencyId = command.CurrencyId;
        Guid? tenancyId = null;

        if (command.PaymentIntent == PaymentIntent.RentPayment)
        {
            var requestedTenancyId = command.TenancyId
                ?? throw new InvalidOperationException("A tenancy id is required to pay rent in-app.");

            var tenancy = await tenancyRepository.FirstOrDefaultAsync(
                new TenancyByIdForClientSpecification(requestedTenancyId, stakeholder.ClientId),
                cancellationToken)
                ?? throw new InvalidOperationException($"Tenancy '{requestedTenancyId}' was not found.");

            if (tenancy.TenantStakeholderId != stakeholder.Id)
            {
                throw new InvalidOperationException("The tenancy does not belong to the authenticated tenant.");
            }

            if (tenancy.Status != TenancyStatus.Active)
            {
                throw new InvalidOperationException("Rent can only be paid for a tenancy with an active rent cycle.");
            }

            var unit = await unitRepository.GetByIdAsync(tenancy.UnitId, cancellationToken)
                ?? throw new InvalidOperationException($"Unit '{tenancy.UnitId}' was not found for tenancy '{tenancy.Id}'.");

            amount = unit.RentAmount;
            currencyId = unit.CurrencyId;
            tenancyId = tenancy.Id;
        }

        var currency = await currencyRepository.FirstOrDefaultAsync(
            new ActiveCurrencyByIdSpecification(currencyId),
            cancellationToken)
            ?? throw new InvalidOperationException($"Currency '{currencyId}' is not active.");

        var supportedCountryCurrency = await countryCurrencyRepository.FirstOrDefaultAsync(
            new CountryCurrencyByCountryAndCurrencySpecification(stakeholder.CountryId, currencyId),
            cancellationToken);

        if (supportedCountryCurrency is null)
        {
            throw new InvalidOperationException(
                $"Currency '{currency.CurrencyCode}' is not supported for country '{stakeholder.CountryId}'.");
        }

        var paymentProvider = await paymentProviderRepository.FirstOrDefaultAsync(
                new ActivePaymentProviderByIdSpecification(command.PaymentProviderId),
                cancellationToken)
            ?? throw new InvalidOperationException($"Payment provider '{command.PaymentProviderId}' is not active.");

        _ = await paymentProviderConfigurationRepository.FirstOrDefaultAsync(
                new EnabledPaymentProviderConfigurationSpecification(command.PaymentProviderId, currencyId, command.PaymentIntent),
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Payment provider '{paymentProvider.ProviderName}' does not support '{currency.CurrencyCode}' for '{command.PaymentIntent}'.");

        var paymentProviderService = paymentProviderServices.SingleOrDefault(service =>
                string.Equals(service.ProviderKey, paymentProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new PaymentProviderResolutionException(
                $"No payment provider service is registered for '{paymentProvider.ProviderKey}'.");

        var merchantReference = $"pay_{Guid.CreateVersion7():N}";

        var paymentTransaction = PaymentTransaction.Create(merchantReference, command.PaymentIntent, paymentProvider.Id, amount, currencyId, stakeholder.CountryId, stakeholder.AppUserId, stakeholder.Id, stakeholder.ClientId, tenancyId);

        await paymentTransactionRepository.AddAsync(paymentTransaction);

        var initiationResult = await paymentProviderService.InitiatePaymentAsync(
            new PaymentProviderInitiationRequest(
                merchantReference,
                amount,
                currency.CurrencyCode,
                command.PaymentIntent,
                stakeholder.Id,
                stakeholder.ClientId,
                stakeholder.CountryId),
            cancellationToken);

        paymentTransaction.MarkInitiated(
            initiationResult.ProviderReference,
            initiationResult.InstructionFields.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal),
            initiationResult.ExpiresAtUtc,
            KnownPaymentTransactionChangeReasons.PaymentInitiated);
        paymentTransaction.SetPaymentMethodType(initiationResult.PaymentMethodType);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.Initiated,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                stakeholder.Id,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Payments.Provider] = paymentProvider.ProviderKey,
                    [Observability.PropertyNames.Payments.PaymentMethod] = paymentTransaction.PaymentMethodType.ToString(),
                    [Observability.PropertyNames.Payments.PaymentIntent] = paymentTransaction.PaymentIntent.ToString(),
                    [Observability.PropertyNames.Payments.MerchantReference] = paymentTransaction.MerchantReference,
                    [Observability.PropertyNames.Payments.ProviderReference] = paymentTransaction.ProviderReference ?? string.Empty,
                    [Observability.PropertyNames.Payments.CurrencyCode] = currency.CurrencyCode
                }));

        return new InitiatePaymentResult(
            paymentTransaction.MerchantReference,
            paymentTransaction.PaymentStatus,
            paymentProvider.Id,
            paymentProvider.ProviderName,
            paymentTransaction.ExpiresAtUtc,
            paymentTransaction.PaymentMethodType,
            initiationResult.InstructionFields);
    }
}



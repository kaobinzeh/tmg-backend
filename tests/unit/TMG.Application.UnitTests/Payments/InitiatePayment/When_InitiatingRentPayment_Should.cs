using TMG.Domain.Common.Auditing;
using TMG.Application.Payments.Features.InitiatePayment;
using TMG.Application.UnitTests.Payments;
using TMG.Contracts.Payments;
using TMG.Domain.Payments;
using TMG.Domain.Payments.Entities;
using TMG.Domain.Payments.Services;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Payments.InitiatePayment;

public sealed class When_InitiatingRentPayment_Should
{
    [Fact]
    public async Task PriceThePaymentFromTheUnitRentAndLinkTheTenancy()
    {
        var context = new PaymentsFlowTestContext();
        var clientId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholder = context.CreateStakeholder(Guid.CreateVersion7(), clientId, countryId);
        var currency = context.CreateCurrency("NGN");
        var countryCurrency = context.CreateCountryCurrency(countryId, currency.Id);
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        provider.SetConfiguration(currency.Id, PaymentIntent.RentPayment, PaymentMethodType.PaymentLink, true);
        var providerConfiguration = provider.Configurations.Single();

        var unit = Unit.Create(Guid.CreateVersion7(), clientId, "Flat 1", null, 2, 1_500_000m, currency.Id);
        var tenancy = Tenancy.Invite(clientId, unit.PropertyId, unit.Id, stakeholder.Id, "tenant@example.com");
        tenancy.Accept(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        tenancy.Activate(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), 12);

        var paymentProviderService = Substitute.For<IPaymentProviderService>();
        PaymentTransaction? capturedTransaction = null;

        context.StakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>()).Returns(stakeholder);
        context.TenancyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Tenancy>>(), Arg.Any<CancellationToken>()).Returns(tenancy);
        context.UnitRepository.GetByIdAsync(unit.Id, Arg.Any<CancellationToken>()).Returns(unit);
        context.CurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Currency>>(), Arg.Any<CancellationToken>()).Returns(currency);
        context.CountryCurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<CountryCurrency>>(), Arg.Any<CancellationToken>()).Returns(countryCurrency);
        context.PaymentProviderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>()).Returns(provider);
        context.PaymentProviderConfigurationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProviderConfiguration>>(), Arg.Any<CancellationToken>()).Returns(providerConfiguration);
        context.PaymentTransactionRepository.AddAsync(Arg.Do<PaymentTransaction>(transaction => capturedTransaction = transaction), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        paymentProviderService.ProviderKey.Returns(PaymentProviderKeys.Credo);
        paymentProviderService.InitiatePaymentAsync(Arg.Any<PaymentProviderInitiationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderInitiationResult(
                "cr_provider_ref",
                PaymentProviderKeys.Credo,
                PaymentMethodType.PaymentLink,
                context.Clock.GetUtcNow().AddMinutes(30),
                new Dictionary<string, string> { ["paymentLink"] = "https://pay.local" }));
        context.PaymentProviderServices.Add(paymentProviderService);

        // Client-sent amount (1) and currency are deliberately wrong — the server prices from the unit.
        var result = await context.CreateInitiatePaymentHandler().HandleAsync(
            new InitiatePaymentCommand(
                1m,
                Guid.CreateVersion7(),
                PaymentIntent.RentPayment,
                provider.Id,
                new ActorContext(stakeholder.Id, clientId, Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N")),
                tenancy.Id),
            CancellationToken.None);

        result.PaymentStatus.ShouldBe(PaymentStatus.Initiated);
        capturedTransaction.ShouldNotBeNull();
        capturedTransaction.PaymentIntent.ShouldBe(PaymentIntent.RentPayment);
        capturedTransaction.Amount.ShouldBe(1_500_000m);
        capturedTransaction.CurrencyId.ShouldBe(currency.Id);
        capturedTransaction.TenancyId.ShouldBe(tenancy.Id);
    }

    [Fact]
    public async Task RejectWhenTheTenancyBelongsToAnotherTenant()
    {
        var context = new PaymentsFlowTestContext();
        var clientId = Guid.CreateVersion7();
        var stakeholder = context.CreateStakeholder(Guid.CreateVersion7(), clientId, Guid.CreateVersion7());
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);

        // Tenancy owned by a different tenant stakeholder.
        var tenancy = Tenancy.Invite(clientId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "other@example.com");

        context.StakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>()).Returns(stakeholder);
        context.TenancyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Tenancy>>(), Arg.Any<CancellationToken>()).Returns(tenancy);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await context.CreateInitiatePaymentHandler().HandleAsync(
                new InitiatePaymentCommand(
                    1m,
                    Guid.CreateVersion7(),
                    PaymentIntent.RentPayment,
                    provider.Id,
                    new ActorContext(stakeholder.Id, clientId, "corr", "flow"),
                    tenancy.Id),
                CancellationToken.None));
    }
}

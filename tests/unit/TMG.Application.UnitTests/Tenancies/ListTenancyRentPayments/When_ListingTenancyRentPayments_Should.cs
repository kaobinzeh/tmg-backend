using TMG.Application.Tenancies.Features.ListTenancyRentPayments;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.ListTenancyRentPayments;

public sealed class When_ListingTenancyRentPayments_Should
{
    [Fact]
    public async Task ReturnMappedPaymentsForATenancyOwnedByTheClient()
    {
        var context = new TenanciesFlowTestContext();
        var clientId = Guid.CreateVersion7();
        var tenancyId = Guid.CreateVersion7();
        var payment = context.CreateRentPaymentReadModel();

        context.TenancyRepository
            .FirstOrDefaultAsync(Arg.Any<TenancyByIdForClientSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Tenancy.Invite(clientId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "tenant@example.com"));

        Guid? capturedClientId = null;
        Guid? capturedTenancyId = null;
        context.TenancyReadModelRepository
            .ListRentPaymentsByTenancyAsync(
                Arg.Do<Guid>(id => capturedClientId = id),
                Arg.Do<Guid>(id => capturedTenancyId = id),
                Arg.Any<CancellationToken>())
            .Returns([payment]);

        var result = await context.CreateListTenancyRentPaymentsHandler().HandleAsync(
            new ListTenancyRentPaymentsCommand(
                tenancyId,
                new ActorContext(Guid.CreateVersion7(), clientId, "corr", "flow")),
            CancellationToken.None);

        capturedClientId.ShouldBe(clientId);
        capturedTenancyId.ShouldBe(tenancyId);
        result.Status.ShouldBe(ListTenancyRentPaymentsStatus.Success);
        result.Payments.Count.ShouldBe(1);
        var item = result.Payments[0];
        item.RentPaymentId.ShouldBe(payment.RentPaymentId);
        item.Amount.ShouldBe(payment.Amount);
        item.CurrencyId.ShouldBe(payment.CurrencyId);
        item.Method.ShouldBe(nameof(RentPaymentMethod.BankTransfer));
        item.Reference.ShouldBe(payment.Reference);
        item.PaidAtUtc.ShouldBe(payment.PaidAtUtc);
        item.PeriodStartUtc.ShouldBe(payment.PeriodStartUtc);
        item.PeriodEndUtc.ShouldBe(payment.PeriodEndUtc);
        item.ReceiptDocumentId.ShouldBe(payment.ReceiptDocumentId);
        item.ReceiptNumber.ShouldBe(RentPayment.FormatReceiptNumber(payment.RentPaymentId));
    }

    [Fact]
    public async Task ReturnTenancyNotFoundWhenTheTenancyDoesNotBelongToTheClient()
    {
        var context = new TenanciesFlowTestContext();
        context.TenancyRepository
            .FirstOrDefaultAsync(Arg.Any<TenancyByIdForClientSpecification>(), Arg.Any<CancellationToken>())
            .Returns((Tenancy?)null);

        var result = await context.CreateListTenancyRentPaymentsHandler().HandleAsync(
            new ListTenancyRentPaymentsCommand(
                Guid.CreateVersion7(),
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListTenancyRentPaymentsStatus.TenancyNotFound);
        result.Payments.ShouldBeEmpty();
        await context.TenancyReadModelRepository.DidNotReceive()
            .ListRentPaymentsByTenancyAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnNotAuthenticatedWhenClientIdIsMissing()
    {
        var context = new TenanciesFlowTestContext();

        var result = await context.CreateListTenancyRentPaymentsHandler().HandleAsync(
            new ListTenancyRentPaymentsCommand(
                Guid.CreateVersion7(),
                new ActorContext(null, null, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListTenancyRentPaymentsStatus.NotAuthenticated);
        result.Payments.ShouldBeEmpty();
        await context.TenancyRepository.DidNotReceive()
            .FirstOrDefaultAsync(Arg.Any<TenancyByIdForClientSpecification>(), Arg.Any<CancellationToken>());
    }
}

using TMG.Consumer.UnitTests.Payments;
using TMG.Contracts.Events;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Consumer.UnitTests.Payments.RecordRentPayment;

public sealed class When_HandlingRecordRentPayment_WithNewPayment_Should
{
    [Fact]
    public async Task SettleTheCycleRecordTheLedgerEntryAndPublishTheReceiptEvent()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();

        var clientId = Guid.CreateVersion7();
        var tenancy = PaymentsConsumerTestContext.CreateActiveTenancy(clientId);
        var command = context.CreateRecordRentPaymentCommand(tenancy.Id, 1_500_000m, Guid.CreateVersion7(), clientId);
        var receiptDocumentId = Guid.CreateVersion7();

        context.RentPaymentRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<RentPayment>>(), Arg.Any<CancellationToken>())
            .Returns((RentPayment?)null);
        context.TenancyRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Tenancy>>(), Arg.Any<CancellationToken>())
            .Returns(tenancy);
        context.StakeholderReadModelRepository
            .GetByStakeholderIdAsync(tenancy.TenantStakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                tenancy.TenantStakeholderId, Guid.CreateVersion7(), "tenant@example.com", clientId,
                Guid.CreateVersion7(), Guid.CreateVersion7(), "tenant", "Tobi", "Ade", null, true));
        context.ReceiptArchiver
            .ArchiveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<RentReceiptModel>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(receiptDocumentId);

        RentPayment? capturedPayment = null;
        context.RentPaymentRepository
            .AddAsync(Arg.Do<RentPayment>(payment => capturedPayment = payment), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await context.CreateRecordRentPaymentHandler().HandleAsync(command, CancellationToken.None);

        capturedPayment.ShouldNotBeNull();
        capturedPayment.PaymentTransactionId.ShouldBe(command.PaymentTransactionId);
        capturedPayment.Amount.ShouldBe(1_500_000m);
        capturedPayment.Method.ShouldBe(RentPaymentMethod.Online);
        capturedPayment.RecordedByStakeholderId.ShouldBeNull();
        capturedPayment.ReceiptDocumentId.ShouldBe(receiptDocumentId);

        // In-app payment settles the current term (first payment) without advancing the cycle.
        tenancy.CycleEndUtc.ShouldBe(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await context.ReceiptArchiver.Received(1).ArchiveAsync(
            command.ClientId, tenancy.Id, Arg.Any<RentReceiptModel>(), null, Arg.Any<CancellationToken>());
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<RentPaymentReceived>(e => e.TenancyId == tenancy.Id && e.RentPaymentId == capturedPayment.Id),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

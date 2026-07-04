using TMG.Consumer.UnitTests.Payments;
using TMG.Contracts.Events;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Consumer.UnitTests.Payments.RecordRentPayment;

public sealed class When_HandlingRecordRentPayment_WithDuplicatePaymentTransaction_Should
{
    [Fact]
    public async Task SkipRecordingAndNotSettleTheCycleAgain()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();

        var clientId = Guid.CreateVersion7();
        var tenancy = PaymentsConsumerTestContext.CreateActiveTenancy(clientId);
        var command = context.CreateRecordRentPaymentCommand(tenancy.Id, 1_500_000m, Guid.CreateVersion7(), clientId);

        var alreadyRecorded = RentPayment.Record(
            clientId, tenancy.Id, tenancy.UnitId, tenancy.PropertyId, 1_500_000m, Guid.CreateVersion7(),
            new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            new RentPeriod(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            RentPaymentMethod.Online, command.MerchantReference, recordedByStakeholderId: null,
            paymentTransactionId: command.PaymentTransactionId);

        context.RentPaymentRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<RentPayment>>(), Arg.Any<CancellationToken>())
            .Returns(alreadyRecorded);

        await context.CreateRecordRentPaymentHandler().HandleAsync(command, CancellationToken.None);

        await context.RentPaymentRepository.DidNotReceive().AddAsync(Arg.Any<RentPayment>(), Arg.Any<CancellationToken>());
        await context.ReceiptArchiver.DidNotReceive().ArchiveAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<RentReceiptModel>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await context.EventPublisher.DidNotReceive().PublishAsync(Arg.Any<RentPaymentReceived>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

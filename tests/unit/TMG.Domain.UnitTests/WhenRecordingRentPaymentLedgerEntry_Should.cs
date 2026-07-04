using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests;

public sealed class WhenRecordingRentPaymentLedgerEntry_Should
{
    private static readonly RentPeriod Period = new(
        new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2028, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private static RentPayment Record(decimal amount) =>
        RentPayment.Record(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            amount, Guid.CreateVersion7(),
            new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero),
            Period, RentPaymentMethod.BankTransfer, "TRF-001", Guid.CreateVersion7());

    [Fact]
    public void CaptureTheAmountPeriodAndMethod()
    {
        var payment = Record(1_500_000m);

        payment.Amount.ShouldBe(1_500_000m);
        payment.PeriodStartUtc.ShouldBe(Period.StartUtc);
        payment.PeriodEndUtc.ShouldBe(Period.EndUtc);
        payment.Method.ShouldBe(RentPaymentMethod.BankTransfer);
        payment.Reference.ShouldBe("TRF-001");
        payment.ReceiptDocumentId.ShouldBeNull();
    }

    [Fact]
    public void LinkTheReceiptDocumentOnceArchived()
    {
        var payment = Record(1_500_000m);
        var documentId = Guid.CreateVersion7();

        payment.AttachReceipt(documentId);

        payment.ReceiptDocumentId.ShouldBe(documentId);
    }

    [Fact]
    public void RejectANonPositiveAmount()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Record(0m));
    }
}

using TMG.Domain.Common.Entities;

namespace TMG.Domain.Tenancies.Entities;

/// <summary>
/// A single rent payment recorded against a tenancy — the ledger backing the legally-required receipt.
/// Currently created by the manager-recorded offline flow; the same aggregate is the anchor for the
/// (future) in-app collection path.
/// </summary>
public sealed class RentPayment : Entity, IAggregateRoot
{
    private const int MaxReferenceLength = 200;

    private RentPayment()
    {
    }

    private RentPayment(
        Guid clientId,
        Guid tenancyId,
        Guid unitId,
        Guid propertyId,
        decimal amount,
        Guid currencyId,
        DateTimeOffset paidAtUtc,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        RentPaymentMethod method,
        string? reference,
        Guid? recordedByStakeholderId,
        Guid? paymentTransactionId)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Rent payment amount must be greater than zero.");
        }

        ClientId = clientId;
        TenancyId = tenancyId;
        UnitId = unitId;
        PropertyId = propertyId;
        Amount = amount;
        CurrencyId = currencyId;
        PaidAtUtc = paidAtUtc;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        Method = method;
        Reference = NormalizeOptional(reference, nameof(reference), MaxReferenceLength);
        RecordedByStakeholderId = recordedByStakeholderId;
        PaymentTransactionId = paymentTransactionId;
    }

    public Guid ClientId { get; private set; }
    public Guid TenancyId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid PropertyId { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CurrencyId { get; private set; }
    public DateTimeOffset PaidAtUtc { get; private set; }

    // The lease term this payment covers.
    public DateTimeOffset PeriodStartUtc { get; private set; }
    public DateTimeOffset PeriodEndUtc { get; private set; }

    public RentPaymentMethod Method { get; private set; }

    /// <summary>Manager-supplied external reference (e.g. bank transfer reference); optional.</summary>
    public string? Reference { get; private set; }

    /// <summary>The manager who recorded the payment, or null for a system-recorded (in-app) payment.</summary>
    public Guid? RecordedByStakeholderId { get; private set; }

    /// <summary>The gateway payment transaction this ledger entry settled, or null for a manager-recorded offline payment.</summary>
    public Guid? PaymentTransactionId { get; private set; }

    /// <summary>The generated receipt document in the vault, once it has been rendered and stored.</summary>
    public Guid? ReceiptDocumentId { get; private set; }

    /// <summary>Human-readable receipt number derived from the ledger id, e.g. <c>RCPT-01AB23CD</c>.</summary>
    public string ReceiptNumber => $"RCPT-{Id.ToString("N")[..8].ToUpperInvariant()}";

    public static RentPayment Record(
        Guid clientId,
        Guid tenancyId,
        Guid unitId,
        Guid propertyId,
        decimal amount,
        Guid currencyId,
        DateTimeOffset paidAtUtc,
        RentPeriod period,
        RentPaymentMethod method,
        string? reference,
        Guid? recordedByStakeholderId,
        Guid? paymentTransactionId = null) =>
        new(clientId, tenancyId, unitId, propertyId, amount, currencyId, paidAtUtc,
            period.StartUtc, period.EndUtc, method, reference, recordedByStakeholderId, paymentTransactionId);

    public void AttachReceipt(Guid receiptDocumentId) => ReceiptDocumentId = receiptDocumentId;

    private static string? NormalizeOptional(string? value, string argumentName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"{argumentName} must not exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }
}

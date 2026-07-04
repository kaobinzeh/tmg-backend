namespace TMG.Contracts.Events;

/// <summary>
/// Raised when a rent payment has been recorded against a tenancy (and its receipt archived). The Consumer
/// turns this into a receipt email to the tenant.
/// </summary>
public sealed record RentPaymentReceived : BaseEvent
{
    public Guid TenancyId { get; init; }
    public Guid UnitId { get; init; }
    public Guid PropertyId { get; init; }
    public Guid RentPaymentId { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public Guid CurrencyId { get; init; }
    public DateTimeOffset PaidAtUtc { get; init; }
    public DateTimeOffset PeriodEndUtc { get; init; }
    public string UnitLabel { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
}

namespace TMG.Domain.Common.Notifications;

/// <summary>Inputs for rendering a rent-payment receipt as a self-contained HTML document.</summary>
public sealed record RentReceiptModel(
    string ReceiptNumber,
    string TenantName,
    string PropertyName,
    string UnitLabel,
    decimal Amount,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    string PaymentMethod,
    string? Reference);

using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.ReadModels;

/// <summary>A recorded rent payment against a tenancy, projected for the manager's payment-history view.</summary>
public sealed record RentPaymentReadModel(
    Guid RentPaymentId,
    decimal Amount,
    Guid CurrencyId,
    RentPaymentMethod Method,
    string? Reference,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    Guid? ReceiptDocumentId);

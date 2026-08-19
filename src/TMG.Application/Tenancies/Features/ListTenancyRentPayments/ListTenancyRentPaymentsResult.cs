using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.ReadModels;

namespace TMG.Application.Tenancies.Features.ListTenancyRentPayments;

public sealed record ListTenancyRentPaymentsResult(
    ListTenancyRentPaymentsStatus Status,
    IReadOnlyList<RentPaymentListItem> Payments);

public sealed record RentPaymentListItem(
    Guid RentPaymentId,
    decimal Amount,
    Guid CurrencyId,
    string Method,
    string? Reference,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    Guid? ReceiptDocumentId,
    string ReceiptNumber)
{
    public static RentPaymentListItem FromReadModel(RentPaymentReadModel payment) =>
        new(
            payment.RentPaymentId,
            payment.Amount,
            payment.CurrencyId,
            payment.Method.ToString(),
            payment.Reference,
            payment.PaidAtUtc,
            payment.PeriodStartUtc,
            payment.PeriodEndUtc,
            payment.ReceiptDocumentId,
            RentPayment.FormatReceiptNumber(payment.RentPaymentId));
}

public enum ListTenancyRentPaymentsStatus
{
    Success = 1,
    NotAuthenticated = 2,
    TenancyNotFound = 3
}

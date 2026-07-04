namespace TMG.Application.Tenancies.Features.RecordRentPayment;

public sealed record RecordRentPaymentResult(
    RecordRentPaymentStatus Status,
    Guid? RentPaymentId = null,
    Guid? ReceiptDocumentId = null);

public enum RecordRentPaymentStatus
{
    Success = 1,
    NotAuthenticated = 2,
    TenancyNotFound = 3,
    NotActive = 4,
    InvalidAmount = 5
}

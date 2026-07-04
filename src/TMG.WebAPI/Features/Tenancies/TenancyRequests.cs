using TMG.Domain.Tenancies.Entities;

namespace TMG.WebAPI.Features.Tenancies;

public sealed record AllocateUnitRequest(
    Guid UnitId,
    string TenantEmail,
    string TenantFirstName,
    string TenantLastName,
    DateTimeOffset? LeaseStartDate = null,
    int? TermMonths = null);

public sealed record ActivateTenancyRequest(
    DateTimeOffset? LeaseStartDate = null,
    int? TermMonths = null);

public sealed record AcceptTenancyInvitationRequest(
    string Email,
    string Token,
    string? Password,
    string? ConfirmPassword,
    bool AcceptedTerms);

public sealed record RejectTenancyInvitationRequest(
    string Email,
    string Token);

public sealed record UploadTenancyDocumentRequest(
    TenancyDocumentType DocumentType,
    IFormFile? File);

public sealed record RecordRentPaymentRequest(
    decimal Amount,
    RentPaymentMethod Method,
    string? Reference = null,
    DateTimeOffset? PaidAtUtc = null);

public sealed record AllocateUnitResponse(Guid TenancyId);

public sealed record UploadTenancyDocumentResponse(Guid DocumentId);

public sealed record RecordRentPaymentResponse(Guid RentPaymentId, Guid? ReceiptDocumentId);

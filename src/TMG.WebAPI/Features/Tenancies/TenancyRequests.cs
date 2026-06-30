using TMG.Domain.Tenancies.Entities;

namespace TMG.WebAPI.Features.Tenancies;

public sealed record AllocateUnitRequest(
    Guid UnitId,
    string TenantEmail,
    string TenantFirstName,
    string TenantLastName);

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

public sealed record AllocateUnitResponse(Guid TenancyId);

public sealed record UploadTenancyDocumentResponse(Guid DocumentId);

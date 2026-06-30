namespace TMG.Application.Tenancies.Features.UploadTenancyDocument;

public sealed record UploadTenancyDocumentResult(
    UploadTenancyDocumentStatus Status,
    Guid? DocumentId = null,
    string? Error = null);

public enum UploadTenancyDocumentStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NoActiveTenancy = 3,
    InvalidFile = 4
}

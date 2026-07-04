using TMG.Domain.Tenancies.Entities;

namespace TMG.Application.Tenancies.Features.ListTenancyDocuments;

public sealed record ListTenancyDocumentsResult(
    ListTenancyDocumentsStatus Status,
    IReadOnlyList<TenancyDocumentListItem>? Documents = null);

public sealed record TenancyDocumentListItem(
    Guid Id,
    TenancyDocumentType DocumentType,
    string ContentType,
    Guid? UploadedByStakeholderId,
    DateTimeOffset CreatedAtUtc);

public enum ListTenancyDocumentsStatus
{
    Success = 1,
    NotAuthenticated = 2,
    TenancyNotFound = 3,
    Forbidden = 4
}

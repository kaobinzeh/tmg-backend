namespace TMG.Application.Tenancies.Features.GetTenancyDocumentDownloadUrl;

public sealed record GetTenancyDocumentDownloadUrlResult(
    GetTenancyDocumentDownloadUrlStatus Status,
    string? Url = null,
    DateTimeOffset? ExpiresAtUtc = null);

public enum GetTenancyDocumentDownloadUrlStatus
{
    Success = 1,
    NotAuthenticated = 2,
    DocumentNotFound = 3,
    Forbidden = 4
}

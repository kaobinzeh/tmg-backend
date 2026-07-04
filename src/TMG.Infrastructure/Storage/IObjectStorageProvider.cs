using TMG.Domain.Common.Storage;

namespace TMG.Infrastructure.Storage;

internal interface IObjectStorageProvider
{
    string ProviderKey { get; }

    Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);
    Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);
    Task<string> GetSignedDownloadUrlAsync(string storageKey, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);
}

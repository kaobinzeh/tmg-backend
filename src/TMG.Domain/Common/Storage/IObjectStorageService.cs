namespace TMG.Domain.Common.Storage;

public interface IObjectStorageService
{
    Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);
    Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Produces a time-limited URL that grants read access to a privately-stored object, given the storage key
    /// returned by <see cref="UploadPrivateAsync"/>.
    /// </summary>
    Task<string> GetSignedDownloadUrlAsync(string storageKey, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);
}

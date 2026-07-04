using TMG.Domain.Common.Storage;

namespace TMG.Infrastructure.Storage;

internal sealed class NoopObjectStorageProvider : IObjectStorageProvider
{
    public string ProviderKey => ObjectStorageProviderKeys.Noop;

    public Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken)
    {
        var escapedKey = NormalizeObjectKey(request.ObjectKey);
        return Task.FromResult($"https://example.invalid/{escapedKey}");
    }

    public Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken)
    {
        var escapedKey = NormalizeObjectKey(request.ObjectKey);
        return Task.FromResult($"https://example.invalid/private/{escapedKey}");
    }

    public Task<string> GetSignedDownloadUrlAsync(string storageKey, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken)
        => Task.FromResult($"{storageKey}?signed=noop&expires={expiresAtUtc.ToUnixTimeSeconds()}");

    private static string NormalizeObjectKey(string objectKey)
        => objectKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);
}

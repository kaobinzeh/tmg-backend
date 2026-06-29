namespace TMG.Domain.Common.Storage;

public interface IObjectStorageService
{
    Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);
    Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);
}

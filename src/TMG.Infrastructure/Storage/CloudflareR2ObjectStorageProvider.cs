using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using TMG.Domain.Common.Storage;
using Microsoft.Extensions.Options;

namespace TMG.Infrastructure.Storage;

internal sealed class CloudflareR2ObjectStorageProvider(IOptions<CloudflareR2Options> options) : IObjectStorageProvider
{
    public string ProviderKey => ObjectStorageProviderKeys.CloudflareR2;

    public async Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken)
    {
        var configuredOptions = options.Value;
        EnsureConfigured(configuredOptions);

        var endpoint = configuredOptions.Endpoint.TrimEnd('/');
        var bucketName = configuredOptions.PublicBucketName.Trim();
        var objectKey = BuildScopedObjectKey(configuredOptions.ApplicationFolder, request.ObjectKey);
        await UploadToBucketAsync(request, configuredOptions, bucketName, objectKey, cancellationToken);

        if (!string.IsNullOrWhiteSpace(configuredOptions.PublicBaseUrl))
        {
            return $"{configuredOptions.PublicBaseUrl.TrimEnd('/')}/{objectKey}";
        }

        return $"{endpoint}/{bucketName}/{objectKey}";
    }

    public async Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken)
    {
        var configuredOptions = options.Value;
        EnsureConfigured(configuredOptions);

        var endpoint = configuredOptions.Endpoint.TrimEnd('/');
        var bucketName = configuredOptions.PrivateBucketName.Trim();
        var objectKey = BuildScopedObjectKey(configuredOptions.ApplicationFolder, request.ObjectKey);
        await UploadToBucketAsync(request, configuredOptions, bucketName, objectKey, cancellationToken);

        return $"{endpoint}/{bucketName}/{objectKey}";
    }

    public async Task<string> GetSignedDownloadUrlAsync(string storageKey, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken)
    {
        var configuredOptions = options.Value;
        EnsureConfigured(configuredOptions);

        var endpoint = configuredOptions.Endpoint.TrimEnd('/');
        var bucketName = configuredOptions.PrivateBucketName.Trim();

        // Private uploads store "{endpoint}/{bucket}/{objectKey}"; recover the object key to presign it.
        var prefix = $"{endpoint}/{bucketName}/";
        var objectKey = storageKey.StartsWith(prefix, StringComparison.Ordinal)
            ? storageKey[prefix.Length..]
            : NormalizeObjectKey(storageKey);

        var credentials = new BasicAWSCredentials(configuredOptions.AccessKeyId, configuredOptions.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };

        using var client = new AmazonS3Client(credentials, config);
        return await client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = expiresAtUtc.UtcDateTime
        });
    }

    private static async Task UploadToBucketAsync(
        ObjectStorageUploadRequest request,
        CloudflareR2Options options,
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken)
    {
        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);

        var config = new AmazonS3Config
        {
            ServiceURL = options.Endpoint.TrimEnd('/'),
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };

        using var client = new AmazonS3Client(credentials, config);
        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = request.Content,
            ContentType = request.ContentType
        };

        await client.PutObjectAsync(putRequest, cancellationToken);
    }

    private static void EnsureConfigured(CloudflareR2Options options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint) ||
            string.IsNullOrWhiteSpace(options.ApplicationFolder) ||
            string.IsNullOrWhiteSpace(options.PublicBucketName) ||
            string.IsNullOrWhiteSpace(options.PrivateBucketName) ||
            string.IsNullOrWhiteSpace(options.AccessKeyId) ||
            string.IsNullOrWhiteSpace(options.SecretAccessKey))
        {
            throw new InvalidOperationException(
                "Cloudflare R2 configuration is incomplete. Ensure Endpoint, ApplicationFolder, PublicBucketName, PrivateBucketName, AccessKeyId, and SecretAccessKey are provided.");
        }
    }

    private static string NormalizeObjectKey(string objectKey)
        => objectKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);

    private static string BuildScopedObjectKey(string applicationFolder, string objectKey)
    {
        var normalizedFolder = NormalizeObjectKey(applicationFolder).TrimEnd('/');
        var normalizedObjectKey = NormalizeObjectKey(objectKey);

        return normalizedObjectKey.StartsWith($"{normalizedFolder}/", StringComparison.Ordinal)
            ? normalizedObjectKey
            : $"{normalizedFolder}/{normalizedObjectKey}";
    }
}

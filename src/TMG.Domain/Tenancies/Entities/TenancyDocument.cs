using TMG.Domain.Common.Entities;

namespace TMG.Domain.Tenancies.Entities;

public sealed class TenancyDocument : Entity, IAggregateRoot
{
    private const int MaxStorageKeyLength = 1024;
    private const int MaxContentTypeLength = 100;

    private TenancyDocument()
    {
    }

    private TenancyDocument(
        Guid clientId,
        Guid tenancyId,
        TenancyDocumentType documentType,
        string storageKey,
        string contentType,
        Guid uploadedByStakeholderId)
    {
        ClientId = clientId;
        TenancyId = tenancyId;
        DocumentType = documentType;
        StorageKey = Normalize(storageKey, nameof(storageKey), MaxStorageKeyLength);
        ContentType = Normalize(contentType, nameof(contentType), MaxContentTypeLength);
        UploadedByStakeholderId = uploadedByStakeholderId;
    }

    public Guid ClientId { get; private set; }
    public Guid TenancyId { get; private set; }
    public TenancyDocumentType DocumentType { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public Guid UploadedByStakeholderId { get; private set; }

    public static TenancyDocument Create(
        Guid clientId,
        Guid tenancyId,
        TenancyDocumentType documentType,
        string storageKey,
        string contentType,
        Guid uploadedByStakeholderId) =>
        new(clientId, tenancyId, documentType, storageKey, contentType, uploadedByStakeholderId);

    private static string Normalize(string value, string argumentName, int maxLength)
    {
        var normalized = value.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException($"{argumentName} is required.", argumentName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"{argumentName} must not exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }
}

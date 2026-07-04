using System.Text;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Infrastructure.Notifications;

/// <summary>
/// Renders the receipt HTML and stores it privately, then records it as a <see cref="TenancyDocument"/> of type
/// <see cref="TenancyDocumentType.Receipt"/>. Mirrors the notice-letter archival flow.
/// </summary>
internal sealed class RentReceiptArchiver(
    IRentReceiptRenderer rentReceiptRenderer,
    IObjectStorageService objectStorageService,
    IRepository<TenancyDocument> tenancyDocumentRepository) : IRentReceiptArchiver
{
    public async Task<Guid> ArchiveAsync(
        Guid clientId,
        Guid tenancyId,
        RentReceiptModel model,
        Guid? uploadedByStakeholderId,
        CancellationToken cancellationToken)
    {
        var receiptHtml = rentReceiptRenderer.Render(model);

        var objectKey =
            $"tenants/{clientId}/tenancies/{tenancyId}/documents/receipt/{Guid.CreateVersion7():N}.html";
        using var receiptStream = new MemoryStream(Encoding.UTF8.GetBytes(receiptHtml));
        var storageKey = await objectStorageService.UploadPrivateAsync(
            new ObjectStorageUploadRequest(objectKey, receiptStream, "text/html"),
            cancellationToken);

        var document = TenancyDocument.Create(
            clientId,
            tenancyId,
            TenancyDocumentType.Receipt,
            storageKey,
            "text/html",
            uploadedByStakeholderId);
        await tenancyDocumentRepository.AddAsync(document, cancellationToken);

        return document.Id;
    }
}

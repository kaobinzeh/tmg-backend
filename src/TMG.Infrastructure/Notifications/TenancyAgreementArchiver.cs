using System.Text;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Infrastructure.Notifications;

/// <summary>
/// Renders the tenancy agreement HTML, stores it privately, and records it as a <see cref="TenancyDocument"/>
/// of type <see cref="TenancyDocumentType.Agreement"/>. Mirrors <see cref="RentReceiptArchiver"/>.
/// </summary>
internal sealed class TenancyAgreementArchiver(
    ITenancyAgreementRenderer tenancyAgreementRenderer,
    IObjectStorageService objectStorageService,
    IRepository<TenancyDocument> tenancyDocumentRepository) : ITenancyAgreementArchiver
{
    public async Task<Guid> ArchiveAsync(
        Guid clientId,
        Guid tenancyId,
        TenancyAgreementModel model,
        CancellationToken cancellationToken)
    {
        var agreementHtml = tenancyAgreementRenderer.Render(model);

        var objectKey =
            $"tenants/{clientId}/tenancies/{tenancyId}/documents/agreement/{Guid.CreateVersion7():N}.html";
        using var agreementStream = new MemoryStream(Encoding.UTF8.GetBytes(agreementHtml));
        var storageKey = await objectStorageService.UploadPrivateAsync(
            new ObjectStorageUploadRequest(objectKey, agreementStream, "text/html"),
            cancellationToken);

        var document = TenancyDocument.Create(
            clientId,
            tenancyId,
            TenancyDocumentType.Agreement,
            storageKey,
            "text/html",
            uploadedByStakeholderId: null);
        await tenancyDocumentRepository.AddAsync(document, cancellationToken);

        return document.Id;
    }
}

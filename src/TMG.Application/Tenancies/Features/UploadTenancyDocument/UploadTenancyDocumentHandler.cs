using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.UploadTenancyDocument;

public sealed class UploadTenancyDocumentHandler(
    IRepository<Tenancy> tenancyRepository,
    IRepository<TenancyDocument> tenancyDocumentRepository,
    IObjectStorageService objectStorageService,
    IUnitOfWork unitOfWork)
{
    private const long MaxDocumentSizeBytes = 10 * 1024 * 1024;

    public async Task<UploadTenancyDocumentResult> HandleAsync(UploadTenancyDocumentCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId ||
            command.ActorContext.StakeholderId is not { } stakeholderId)
        {
            return new UploadTenancyDocumentResult(UploadTenancyDocumentStatus.NotAuthenticated);
        }

        if (command.ContentLength <= 0 ||
            command.ContentLength > MaxDocumentSizeBytes ||
            string.IsNullOrWhiteSpace(command.ContentType) ||
            !(command.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
              command.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)))
        {
            return new UploadTenancyDocumentResult(
                UploadTenancyDocumentStatus.InvalidFile,
                Error: "Document must be an image or PDF up to 10 MB.");
        }

        var tenancy = await tenancyRepository.FirstOrDefaultAsync(
            new AcceptedTenancyByTenantSpecification(stakeholderId),
            cancellationToken);
        if (tenancy is null)
        {
            return new UploadTenancyDocumentResult(UploadTenancyDocumentStatus.NoActiveTenancy);
        }

        var fileExtension = Path.GetExtension(command.FileName);
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            fileExtension = ".bin";
        }

        var objectKey =
            $"tenants/{clientId}/tenancies/{tenancy.Id}/documents/{command.DocumentType.ToString().ToLowerInvariant()}/{Guid.CreateVersion7():N}{fileExtension.ToLowerInvariant()}";
        var storageKey = await objectStorageService.UploadPrivateAsync(
            new ObjectStorageUploadRequest(objectKey, command.Content, command.ContentType),
            cancellationToken);

        var document = TenancyDocument.Create(
            clientId,
            tenancy.Id,
            command.DocumentType,
            storageKey,
            command.ContentType,
            stakeholderId);
        await tenancyDocumentRepository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UploadTenancyDocumentResult(UploadTenancyDocumentStatus.Success, document.Id);
    }
}

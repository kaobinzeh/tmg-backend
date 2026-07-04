using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.GetTenancyDocumentDownloadUrl;

public sealed class GetTenancyDocumentDownloadUrlHandler(
    IRepository<TenancyDocument> tenancyDocumentRepository,
    IRepository<Tenancy> tenancyRepository,
    TenancyDocumentAccessGuard accessGuard,
    IObjectStorageService objectStorageService,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan DownloadUrlValidity = TimeSpan.FromMinutes(15);

    public async Task<GetTenancyDocumentDownloadUrlResult> HandleAsync(GetTenancyDocumentDownloadUrlCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId ||
            command.ActorContext.StakeholderId is not { } stakeholderId)
        {
            return new GetTenancyDocumentDownloadUrlResult(GetTenancyDocumentDownloadUrlStatus.NotAuthenticated);
        }

        var document = await tenancyDocumentRepository.FirstOrDefaultAsync(
            new TenancyDocumentByIdForClientSpecification(command.DocumentId, clientId),
            cancellationToken);
        if (document is null)
        {
            return new GetTenancyDocumentDownloadUrlResult(GetTenancyDocumentDownloadUrlStatus.DocumentNotFound);
        }

        var tenancy = await tenancyRepository.FirstOrDefaultAsync(
            new TenancyByIdForClientSpecification(document.TenancyId, clientId),
            cancellationToken);
        if (tenancy is null)
        {
            return new GetTenancyDocumentDownloadUrlResult(GetTenancyDocumentDownloadUrlStatus.DocumentNotFound);
        }

        if (!await accessGuard.CanAccessAsync(clientId, stakeholderId, tenancy, cancellationToken))
        {
            return new GetTenancyDocumentDownloadUrlResult(GetTenancyDocumentDownloadUrlStatus.Forbidden);
        }

        var expiresAtUtc = timeProvider.GetUtcNow().Add(DownloadUrlValidity);
        var url = await objectStorageService.GetSignedDownloadUrlAsync(document.StorageKey, expiresAtUtc, cancellationToken);

        return new GetTenancyDocumentDownloadUrlResult(GetTenancyDocumentDownloadUrlStatus.Success, url, expiresAtUtc);
    }
}

using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.ListTenancyDocuments;

public sealed class ListTenancyDocumentsHandler(
    IRepository<Tenancy> tenancyRepository,
    IRepository<TenancyDocument> tenancyDocumentRepository,
    TenancyDocumentAccessGuard accessGuard)
{
    public async Task<ListTenancyDocumentsResult> HandleAsync(ListTenancyDocumentsCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId ||
            command.ActorContext.StakeholderId is not { } stakeholderId)
        {
            return new ListTenancyDocumentsResult(ListTenancyDocumentsStatus.NotAuthenticated);
        }

        var tenancy = await tenancyRepository.FirstOrDefaultAsync(
            new TenancyByIdForClientSpecification(command.TenancyId, clientId),
            cancellationToken);
        if (tenancy is null)
        {
            return new ListTenancyDocumentsResult(ListTenancyDocumentsStatus.TenancyNotFound);
        }

        if (!await accessGuard.CanAccessAsync(clientId, stakeholderId, tenancy, cancellationToken))
        {
            return new ListTenancyDocumentsResult(ListTenancyDocumentsStatus.Forbidden);
        }

        var documents = await tenancyDocumentRepository.ListAsync(
            new TenancyDocumentsByTenancySpecification(command.TenancyId, clientId),
            cancellationToken);

        var items = documents
            .Select(document => new TenancyDocumentListItem(
                document.Id,
                document.DocumentType,
                document.ContentType,
                document.UploadedByStakeholderId,
                document.CreatedAtUtc))
            .ToList();

        return new ListTenancyDocumentsResult(ListTenancyDocumentsStatus.Success, items);
    }
}

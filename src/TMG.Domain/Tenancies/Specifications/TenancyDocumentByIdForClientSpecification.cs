using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.Specifications;

/// <summary>A single tenancy document scoped to the owning client.</summary>
public sealed class TenancyDocumentByIdForClientSpecification : Specification<TenancyDocument>
{
    public TenancyDocumentByIdForClientSpecification(Guid documentId, Guid clientId)
    {
        Where(document => document.Id == documentId && document.ClientId == clientId);
    }
}

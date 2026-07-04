using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.Specifications;

/// <summary>All documents archived for a tenancy, newest first, scoped to the owning client.</summary>
public sealed class TenancyDocumentsByTenancySpecification : Specification<TenancyDocument>
{
    public TenancyDocumentsByTenancySpecification(Guid tenancyId, Guid clientId)
    {
        Where(document => document.TenancyId == tenancyId && document.ClientId == clientId);
        ApplyOrderByDescending(document => document.CreatedAtUtc);
    }
}

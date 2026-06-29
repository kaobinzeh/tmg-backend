using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;

namespace TMG.Domain.Stakeholders.Specifications;

public sealed class ClientByIdSpecification : Specification<Client>
{
    public ClientByIdSpecification(Guid clientId)
    {
        Where(client => client.Id == clientId);
    }
}

using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class ClientEmailBaseTemplateByClientIdSpecification : Specification<ClientEmailBaseTemplate>
{
    public ClientEmailBaseTemplateByClientIdSpecification(Guid clientId)
    {
        Where(template => template.ClientId == clientId);
    }
}

using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class EmailNotificationLogsByProviderMessageIdsSpecification : Specification<EmailNotificationLog>
{
    public EmailNotificationLogsByProviderMessageIdsSpecification(IReadOnlyCollection<string> providerMessageIds)
    {
        Where(log => log.ProviderMessageId != null && providerMessageIds.Contains(log.ProviderMessageId));
    }
}

using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class EmailNotificationLogByProviderMessageIdSpecification : Specification<EmailNotificationLog>
{
    public EmailNotificationLogByProviderMessageIdSpecification(string providerMessageId)
    {
        Where(log => log.ProviderMessageId == providerMessageId);
    }
}

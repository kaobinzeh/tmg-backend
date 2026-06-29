using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class EmailNotificationLogByMessageIdSpecification : Specification<EmailNotificationLog>
{
    public EmailNotificationLogByMessageIdSpecification(Guid messageId)
    {
        Where(log => log.MessageId == messageId);
    }
}

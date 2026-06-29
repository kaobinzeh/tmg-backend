using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class EmailNotificationTemplateByNotificationTypeSpecification : Specification<EmailNotificationTemplate>
{
    public EmailNotificationTemplateByNotificationTypeSpecification(NotificationType notificationType)
    {
        Where(template => template.NotificationType == notificationType);
    }
}

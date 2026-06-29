using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Common.Entities;

namespace TMG.Domain.Notifications.Entities;

public sealed class EmailNotificationTemplate : Entity, IAggregateRoot
{
    private EmailNotificationTemplate()
    {
    }

    private EmailNotificationTemplate(
        NotificationType notificationType,
        string description,
        string subject,
        string templateFileName)
    {
        NotificationType = notificationType;
        Description = description.Trim();
        Subject = subject.Trim();
        TemplateFileName = templateFileName.Trim();
    }

    public NotificationType NotificationType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string TemplateFileName { get; private set; } = string.Empty;

    public static EmailNotificationTemplate Create(
        NotificationType notificationType,
        string description,
        string subject,
        string templateFileName) =>
        new(notificationType, description, subject, templateFileName);
}

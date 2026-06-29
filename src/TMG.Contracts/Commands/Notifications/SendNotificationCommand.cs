namespace TMG.Contracts.Commands.Notifications;

public sealed record SendNotificationCommand : BaseCommand
{
    public SendNotificationCommand(
        Guid clientId,
        Guid countryId,
        NotificationType notificationType,
        NotificationMedium notificationMedium,
        NotificationContent notificationContent)
    {
        ClientId = clientId;
        CountryId = countryId;
        NotificationType = notificationType;
        NotificationMedium = notificationMedium;
        NotificationContent = notificationContent;
    }

    public Guid CountryId { get; }
    public NotificationType NotificationType { get; }
    public NotificationMedium NotificationMedium { get; }
    public NotificationContent NotificationContent { get; }
}

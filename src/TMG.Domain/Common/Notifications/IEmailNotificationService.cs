using TMG.Contracts.Commands.Notifications;

namespace TMG.Domain.Common.Notifications;

public interface IEmailNotificationService
{
    Task<EmailNotificationSendResult?> SendAsync(SendNotificationCommand command, CancellationToken cancellationToken = default);
}

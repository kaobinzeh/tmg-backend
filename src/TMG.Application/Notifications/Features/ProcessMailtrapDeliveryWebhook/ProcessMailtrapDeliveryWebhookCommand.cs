namespace TMG.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed record ProcessMailtrapDeliveryWebhookCommand(
    IReadOnlyCollection<MailtrapDeliveryWebhookEvent> Events,
    string RawPayload,
    string? SignatureHeader);

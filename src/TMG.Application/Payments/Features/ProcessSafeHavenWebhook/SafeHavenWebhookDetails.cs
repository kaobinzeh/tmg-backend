namespace TMG.Application.Payments.Features.ProcessSafeHavenWebhook;

public sealed record SafeHavenWebhookDetails(
    string? MerchantReference,
    string? ProviderReference,
    string WebhookEventName,
    string? WebhookEventId,
    bool IsSupportedEvent);

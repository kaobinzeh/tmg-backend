using TMG.Domain.Common.Entities;

namespace TMG.Domain.Notifications.Entities;

public sealed class ClientEmailBaseTemplate : Entity
{
    private ClientEmailBaseTemplate()
    {
    }

    private ClientEmailBaseTemplate(
        Guid clientId,
        string description,
        string htmlTemplate)
    {
        ClientId = clientId;
        Description = description.Trim();
        HtmlTemplate = htmlTemplate.Trim();
    }

    public Guid ClientId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string HtmlTemplate { get; private set; } = string.Empty;

    public static ClientEmailBaseTemplate Create(
        Guid clientId,
        string description,
        string htmlTemplate) =>
        new(clientId, description, htmlTemplate);
}

using TMG.Domain.Common.Entities;

namespace TMG.Domain.Stakeholders.Entities;

public sealed class Client : Entity, IAggregateRoot
{
    private Client()
    {
    }

    private Client(
        Guid id,
        string name,
        string brandKey)
    {
        Id = id;
        Name = name.Trim();
        BrandKey = brandKey.Trim().ToLowerInvariant();
    }

    public string Name { get; private set; } = string.Empty;
    public string BrandKey { get; private set; } = string.Empty;

    public static Client Create(
        Guid id,
        string name,
        string brandKey) =>
        new(id, name, brandKey);
}

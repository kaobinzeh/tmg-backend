using TMG.Domain.Common.Entities;

namespace TMG.Domain.Stakeholders.Entities;

public sealed class StakeholderType : Entity, IAggregateRoot
{
    private StakeholderType()
    {
    }

    private StakeholderType(Guid clientId, string name, string key)
    {
        ClientId = clientId;
        Name = name.Trim();
        Key = key.Trim();
    }

    public Guid ClientId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;

    public ICollection<Stakeholder> Stakeholders { get; private set; } = [];

    public static StakeholderType Create(Guid clientId, string name, string key) =>
        new(clientId, name, key);
}

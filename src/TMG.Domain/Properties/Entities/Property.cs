using TMG.Domain.Common.Entities;

namespace TMG.Domain.Properties.Entities;

public sealed class Property : Entity, IAggregateRoot
{
    private const int MaxNameLength = 200;
    private const int MaxAddressLength = 500;
    private const int MaxDescriptionLength = 2000;

    private Property()
    {
    }

    private Property(
        Guid clientId,
        Guid ownerStakeholderId,
        string name,
        string address,
        string? description)
    {
        ClientId = clientId;
        OwnerStakeholderId = ownerStakeholderId;
        Name = Normalize(name, nameof(name), MaxNameLength);
        Address = Normalize(address, nameof(address), MaxAddressLength);
        Description = NormalizeOptional(description, nameof(description), MaxDescriptionLength);
    }

    public Guid ClientId { get; private set; }
    public Guid OwnerStakeholderId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public static Property Create(
        Guid clientId,
        Guid ownerStakeholderId,
        string name,
        string address,
        string? description) =>
        new(clientId, ownerStakeholderId, name, address, description);

    public void UpdateDetails(string name, string address, string? description)
    {
        Name = Normalize(name, nameof(name), MaxNameLength);
        Address = Normalize(address, nameof(address), MaxAddressLength);
        Description = NormalizeOptional(description, nameof(description), MaxDescriptionLength);
    }

    private static string Normalize(string value, string argumentName, int maxLength)
    {
        var normalized = value.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException($"{argumentName} is required.", argumentName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"{argumentName} must not exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, string argumentName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"{argumentName} must not exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }
}

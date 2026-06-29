using TMG.Domain.Common.Entities;

namespace TMG.Domain.Properties.Entities;

public sealed class Unit : Entity, IAggregateRoot
{
    private const int MaxLabelLength = 100;
    private const int MaxDescriptionLength = 2000;

    private Unit()
    {
    }

    private Unit(
        Guid propertyId,
        Guid clientId,
        string label,
        string? description,
        int numberOfRooms,
        decimal rentAmount,
        Guid currencyId)
    {
        PropertyId = propertyId;
        ClientId = clientId;
        Label = Normalize(label, nameof(label), MaxLabelLength);
        Description = NormalizeOptional(description, nameof(description), MaxDescriptionLength);
        NumberOfRooms = EnsurePositive(numberOfRooms, nameof(numberOfRooms));
        RentAmount = EnsureNonNegative(rentAmount, nameof(rentAmount));
        CurrencyId = currencyId;
        Status = UnitStatus.Available;
    }

    public Guid PropertyId { get; private set; }
    public Guid ClientId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int NumberOfRooms { get; private set; }
    public decimal RentAmount { get; private set; }
    public Guid CurrencyId { get; private set; }
    public UnitStatus Status { get; private set; }

    public static Unit Create(
        Guid propertyId,
        Guid clientId,
        string label,
        string? description,
        int numberOfRooms,
        decimal rentAmount,
        Guid currencyId) =>
        new(propertyId, clientId, label, description, numberOfRooms, rentAmount, currencyId);

    public void UpdateRent(decimal rentAmount) =>
        RentAmount = EnsureNonNegative(rentAmount, nameof(rentAmount));

    public void MarkAvailable() => Status = UnitStatus.Available;

    public void MarkOccupied() => Status = UnitStatus.Occupied;

    public void MarkUnavailable() => Status = UnitStatus.Unavailable;

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

    private static int EnsurePositive(int value, string argumentName)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"{argumentName} must be greater than zero.", argumentName);
        }

        return value;
    }

    private static decimal EnsureNonNegative(decimal value, string argumentName)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{argumentName} must not be negative.", argumentName);
        }

        return value;
    }
}

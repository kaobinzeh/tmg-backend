using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Entities;

namespace TMG.Domain.Stakeholders.Entities;

public sealed class Stakeholder : Entity, IAggregateRoot
{
    private const int MaxFirstNameLength = 100;
    private const int MaxLastNameLength = 100;
    private const int MaxAvatarUrlLength = 2048;

    private Stakeholder()
    {
    }

    private Stakeholder(
        Guid appUserId,
        Guid clientId,
        Guid countryId,
        Guid stakeholderTypeId,
        string firstName,
        string lastName)
    {
        AppUserId = appUserId;
        ClientId = clientId;
        CountryId = countryId;
        StakeholderTypeId = stakeholderTypeId;
        FirstName = NormalizeName(firstName, nameof(firstName), MaxFirstNameLength);
        LastName = NormalizeName(lastName, nameof(lastName), MaxLastNameLength);
        IsVerified = false;
    }

    public Guid AppUserId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid CountryId { get; private set; }
    public Guid StakeholderTypeId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public bool IsVerified { get; private set; }
    public AppUser AppUser { get; private set; } = null!;
    public StakeholderType StakeholderType { get; private set; } = null!;

    public static Stakeholder Create(
        Guid appUserId,
        Guid clientId,
        Guid countryId,
        Guid stakeholderTypeId,
        string firstName,
        string lastName) =>
        new(appUserId, clientId, countryId, stakeholderTypeId, firstName, lastName);

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = NormalizeName(firstName, nameof(firstName), MaxFirstNameLength);
        LastName = NormalizeName(lastName, nameof(lastName), MaxLastNameLength);
    }

    public void SetAvatarUrl(string avatarUrl)
    {
        var normalizedAvatarUrl = avatarUrl.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAvatarUrl))
        {
            throw new ArgumentException("Avatar URL is required.", nameof(avatarUrl));
        }

        if (normalizedAvatarUrl.Length > MaxAvatarUrlLength)
        {
            throw new ArgumentException($"Avatar URL must not exceed {MaxAvatarUrlLength} characters.", nameof(avatarUrl));
        }

        AvatarUrl = normalizedAvatarUrl;
    }

    public void MarkVerified()
    {
        IsVerified = true;
    }

    private static string NormalizeName(string value, string argumentName, int maxLength)
    {
        var normalized = value.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Name is required.", argumentName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Name must not exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }
}

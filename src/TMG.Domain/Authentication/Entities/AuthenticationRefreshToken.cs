using TMG.Domain.Common.Entities;

namespace TMG.Domain.Authentication.Entities;

public sealed class AuthenticationRefreshToken : Entity, IAggregateRoot
{
    private AuthenticationRefreshToken()
    {
    }

    private AuthenticationRefreshToken(
        Guid appUserId,
        string tokenHash,
        string securityStamp,
        DateTimeOffset expiresAtUtc)
    {
        AppUserId = appUserId;
        TokenHash = tokenHash;
        SecurityStamp = securityStamp;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid AppUserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string SecurityStamp { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public AppUser AppUser { get; private set; } = null!;

    public static AuthenticationRefreshToken Create(
        Guid appUserId,
        string tokenHash,
        string securityStamp,
        DateTimeOffset expiresAtUtc) =>
        new(appUserId, tokenHash, securityStamp, expiresAtUtc);

    public bool CanBeRedeemed(string securityStamp, DateTimeOffset utcNow) =>
        RevokedAtUtc is null
        && ExpiresAtUtc > utcNow
        && string.Equals(SecurityStamp, securityStamp ?? string.Empty, StringComparison.Ordinal);

    public void Revoke(DateTimeOffset utcNow)
    {
        if (RevokedAtUtc.HasValue)
        {
            return;
        }

        RevokedAtUtc = utcNow;
    }
}

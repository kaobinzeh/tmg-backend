namespace TMG.Domain.Common.Authentication;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc)
{
    public const string StakeholderIdClaimType = "stakeholder_id";
    public const string TokenIdClaimType = "jti";
}

using TMG.Domain.Common.Entities;

namespace TMG.Domain.Tenancies.Entities;

public sealed class Tenancy : Entity, IAggregateRoot
{
    private const int MaxEmailLength = 256;

    private Tenancy()
    {
    }

    private Tenancy(
        Guid clientId,
        Guid propertyId,
        Guid unitId,
        Guid tenantStakeholderId,
        string invitedEmail)
    {
        ClientId = clientId;
        PropertyId = propertyId;
        UnitId = unitId;
        TenantStakeholderId = tenantStakeholderId;
        InvitedEmail = NormalizeEmail(invitedEmail);
        Status = TenancyStatus.Invited;
    }

    public Guid ClientId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid TenantStakeholderId { get; private set; }
    public string InvitedEmail { get; private set; } = string.Empty;
    public TenancyStatus Status { get; private set; }
    public DateTimeOffset? TermsAcceptedAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? RejectedAtUtc { get; private set; }

    public static Tenancy Invite(
        Guid clientId,
        Guid propertyId,
        Guid unitId,
        Guid tenantStakeholderId,
        string invitedEmail) =>
        new(clientId, propertyId, unitId, tenantStakeholderId, invitedEmail);

    public void Accept(DateTimeOffset acceptedAtUtc)
    {
        EnsureInvited();
        Status = TenancyStatus.Accepted;
        TermsAcceptedAtUtc = acceptedAtUtc;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public void Reject(DateTimeOffset rejectedAtUtc)
    {
        EnsureInvited();
        Status = TenancyStatus.Rejected;
        RejectedAtUtc = rejectedAtUtc;
    }

    private void EnsureInvited()
    {
        if (Status != TenancyStatus.Invited)
        {
            throw new InvalidOperationException($"Tenancy '{Id}' is not awaiting a response (current status: {Status}).");
        }
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Invited email is required.", nameof(email));
        }

        if (normalized.Length > MaxEmailLength)
        {
            throw new ArgumentException($"Invited email must not exceed {MaxEmailLength} characters.", nameof(email));
        }

        return normalized;
    }
}

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
        string invitedEmail,
        DateTimeOffset? proposedLeaseStartUtc,
        int? proposedTermMonths)
    {
        ClientId = clientId;
        PropertyId = propertyId;
        UnitId = unitId;
        TenantStakeholderId = tenantStakeholderId;
        InvitedEmail = NormalizeEmail(invitedEmail);
        Status = TenancyStatus.Invited;
        ProposedLeaseStartUtc = proposedLeaseStartUtc;
        ProposedTermMonths = proposedTermMonths;
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

    // Lease terms proposed by the manager at allocation; used as defaults when the cycle is activated.
    public DateTimeOffset? ProposedLeaseStartUtc { get; private set; }
    public int? ProposedTermMonths { get; private set; }

    // Rent cycle — set once the tenancy is activated.
    public DateTimeOffset? CycleStartUtc { get; private set; }
    public DateTimeOffset? CycleEndUtc { get; private set; }
    public DateTimeOffset? NextRentDueUtc { get; private set; }
    public DateTimeOffset? Reminder3MonthsSentAtUtc { get; private set; }
    public DateTimeOffset? Reminder1MonthSentAtUtc { get; private set; }

    public static Tenancy Invite(
        Guid clientId,
        Guid propertyId,
        Guid unitId,
        Guid tenantStakeholderId,
        string invitedEmail,
        DateTimeOffset? proposedLeaseStartUtc = null,
        int? proposedTermMonths = null) =>
        new(clientId, propertyId, unitId, tenantStakeholderId, invitedEmail, proposedLeaseStartUtc, proposedTermMonths);

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

    /// <summary>
    /// Starts the rent cycle for an accepted tenancy: rent for the next term falls due when the current term ends.
    /// Re-arms the renewal reminders so a future cycle advance fires them again.
    /// </summary>
    public void Activate(DateTimeOffset startUtc, int termMonths)
    {
        if (Status != TenancyStatus.Accepted)
        {
            throw new InvalidOperationException(
                $"Tenancy '{Id}' must be accepted before its rent cycle can be activated (current status: {Status}).");
        }

        if (termMonths <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(termMonths), "Lease term must be greater than zero months.");
        }

        Status = TenancyStatus.Active;
        CycleStartUtc = startUtc;
        CycleEndUtc = startUtc.AddMonths(termMonths);
        NextRentDueUtc = CycleEndUtc;
        Reminder3MonthsSentAtUtc = null;
        Reminder1MonthSentAtUtc = null;
    }

    public void RecordReminderSent(RentReminderKind kind, DateTimeOffset sentAtUtc)
    {
        switch (kind)
        {
            case RentReminderKind.RentDueReminder3Months:
                Reminder3MonthsSentAtUtc = sentAtUtc;
                break;
            case RentReminderKind.RentDueReminder1Month:
                Reminder1MonthSentAtUtc = sentAtUtc;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown rent reminder kind.");
        }
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

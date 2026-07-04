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
    public int? TermMonths { get; private set; }
    public DateTimeOffset? LastRentPaidAtUtc { get; private set; }
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
        TermMonths = termMonths;
        Reminder3MonthsSentAtUtc = null;
        Reminder1MonthSentAtUtc = null;
    }

    /// <summary>
    /// Records a rent payment against the active cycle and returns the lease period it covers.
    /// The first payment settles the current (initial) term the cycle already frames — the upcoming renewal
    /// date is unchanged. Every subsequent payment is a renewal that rolls the cycle forward by one term and
    /// re-arms the renewal reminders.
    /// </summary>
    public RentPeriod RecordRentPayment(DateTimeOffset paidAtUtc)
    {
        if (Status != TenancyStatus.Active)
        {
            throw new InvalidOperationException(
                $"Tenancy '{Id}' must have an active rent cycle before a payment can be recorded (current status: {Status}).");
        }

        if (TermMonths is not { } term || CycleStartUtc is not { } currentStart || CycleEndUtc is not { } currentEnd)
        {
            throw new InvalidOperationException($"Tenancy '{Id}' does not have an initialized rent cycle.");
        }

        RentPeriod period;
        if (LastRentPaidAtUtc is null)
        {
            // First payment settles the current term the cycle already frames; NextRentDueUtc (the upcoming
            // renewal, == CycleEndUtc) and the armed reminders stay as they are.
            period = new RentPeriod(currentStart, currentEnd);
        }
        else
        {
            // Renewal: roll the cycle forward by one term and re-arm the renewal reminders.
            var nextEnd = currentEnd.AddMonths(term);
            period = new RentPeriod(currentEnd, nextEnd);
            CycleStartUtc = currentEnd;
            CycleEndUtc = nextEnd;
            NextRentDueUtc = nextEnd;
            Reminder3MonthsSentAtUtc = null;
            Reminder1MonthSentAtUtc = null;
        }

        LastRentPaidAtUtc = paidAtUtc;
        return period;
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

using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests;

public sealed class WhenRecordingRentPayment_Should
{
    private static readonly DateTimeOffset CycleStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Tenancy ActiveTenancy()
    {
        var tenancy = Tenancy.Invite(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "tenant@example.com");
        tenancy.Accept(CycleStart);
        tenancy.Activate(CycleStart, termMonths: 12);
        return tenancy;
    }

    [Fact]
    public void SettleTheCurrentTermOnTheFirstPaymentWithoutAdvancingTheCycle()
    {
        var tenancy = ActiveTenancy();
        var paidAt = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var period = tenancy.RecordRentPayment(paidAt);

        // The initial term the active cycle already frames.
        period.StartUtc.ShouldBe(CycleStart);                // 2026-01-01
        period.EndUtc.ShouldBe(CycleStart.AddMonths(12));    // 2027-01-01
        // Cycle + upcoming renewal date are unchanged.
        tenancy.CycleStartUtc.ShouldBe(CycleStart);
        tenancy.CycleEndUtc.ShouldBe(CycleStart.AddMonths(12));
        tenancy.NextRentDueUtc.ShouldBe(CycleStart.AddMonths(12));
        tenancy.LastRentPaidAtUtc.ShouldBe(paidAt);
    }

    [Fact]
    public void RollTheCycleForwardOnTheRenewalPayment()
    {
        var tenancy = ActiveTenancy();
        tenancy.RecordRentPayment(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero)); // initial term settled
        var renewalPaidAt = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

        var period = tenancy.RecordRentPayment(renewalPaidAt);

        period.StartUtc.ShouldBe(CycleStart.AddMonths(12)); // 2027-01-01
        period.EndUtc.ShouldBe(CycleStart.AddMonths(24));   // 2028-01-01
        tenancy.CycleStartUtc.ShouldBe(CycleStart.AddMonths(12));
        tenancy.CycleEndUtc.ShouldBe(CycleStart.AddMonths(24));
        tenancy.NextRentDueUtc.ShouldBe(CycleStart.AddMonths(24));
        tenancy.LastRentPaidAtUtc.ShouldBe(renewalPaidAt);
    }

    [Fact]
    public void LeaveRemindersUntouchedOnTheFirstPaymentButRearmThemOnRenewal()
    {
        var tenancy = ActiveTenancy();
        tenancy.RecordReminderSent(RentReminderKind.RentDueReminder3Months, CycleStart);
        tenancy.RecordReminderSent(RentReminderKind.RentDueReminder1Month, CycleStart);

        // First payment settles the current term — the renewal reminders remain as they were.
        tenancy.RecordRentPayment(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        tenancy.Reminder3MonthsSentAtUtc.ShouldNotBeNull();
        tenancy.Reminder1MonthSentAtUtc.ShouldNotBeNull();

        // Renewal payment rolls the cycle forward and re-arms the reminders.
        tenancy.RecordRentPayment(new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));
        tenancy.Reminder3MonthsSentAtUtc.ShouldBeNull();
        tenancy.Reminder1MonthSentAtUtc.ShouldBeNull();
    }

    [Fact]
    public void RejectRecordingWhenTheCycleIsNotActive()
    {
        var invitedOnly = Tenancy.Invite(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "tenant@example.com");

        Should.Throw<InvalidOperationException>(() =>
            invitedOnly.RecordRentPayment(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero)));
    }
}

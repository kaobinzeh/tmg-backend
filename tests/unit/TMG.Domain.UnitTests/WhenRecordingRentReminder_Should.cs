using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests;

public sealed class WhenRecordingRentReminder_Should
{
    private static Tenancy ActiveTenancy()
    {
        var tenancy = Tenancy.Invite(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "tenant@example.com");
        tenancy.Accept(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        tenancy.Activate(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), 12);
        return tenancy;
    }

    [Fact]
    public void StampTheThreeMonthReminderOnly()
    {
        var tenancy = ActiveTenancy();
        var sentAt = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

        tenancy.RecordReminderSent(RentReminderKind.RentDueReminder3Months, sentAt);

        tenancy.Reminder3MonthsSentAtUtc.ShouldBe(sentAt);
        tenancy.Reminder1MonthSentAtUtc.ShouldBeNull();
    }

    [Fact]
    public void StampTheOneMonthReminderOnly()
    {
        var tenancy = ActiveTenancy();
        var sentAt = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

        tenancy.RecordReminderSent(RentReminderKind.RentDueReminder1Month, sentAt);

        tenancy.Reminder1MonthSentAtUtc.ShouldBe(sentAt);
        tenancy.Reminder3MonthsSentAtUtc.ShouldBeNull();
    }
}

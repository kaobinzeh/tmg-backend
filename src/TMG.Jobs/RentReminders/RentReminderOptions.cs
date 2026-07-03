namespace TMG.Jobs.RentReminders;

public sealed class RentReminderOptions
{
    public const string SectionName = "Jobs:Tenancies:RentReminders";

    public int BatchSize { get; set; } = 100;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromHours(6);
}

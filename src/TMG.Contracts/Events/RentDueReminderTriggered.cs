using TMG.Contracts.Commands.Notifications;

namespace TMG.Contracts.Events;

public sealed record RentDueReminderTriggered : BaseEvent
{
    public Guid TenancyId { get; init; }
    public Guid UnitId { get; init; }
    public Guid PropertyId { get; init; }

    /// <summary>Which reminder this is — also selects the email template and notice wording.</summary>
    public NotificationType ReminderType { get; init; }

    public DateTimeOffset NextRentDueUtc { get; init; }
    public string UnitLabel { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public decimal RentAmount { get; init; }
    public Guid CurrencyId { get; init; }
}

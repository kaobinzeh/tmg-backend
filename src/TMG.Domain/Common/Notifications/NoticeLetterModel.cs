namespace TMG.Domain.Common.Notifications;

/// <summary>Inputs for rendering a formal rent-due notice letter.</summary>
public sealed record NoticeLetterModel(
    string TenantName,
    string PropertyName,
    string UnitLabel,
    decimal RentAmount,
    DateTimeOffset NextRentDueUtc,
    string NoticePeriodLabel);

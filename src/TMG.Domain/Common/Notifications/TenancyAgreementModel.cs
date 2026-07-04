namespace TMG.Domain.Common.Notifications;

/// <summary>Inputs for rendering a tenancy agreement as a self-contained HTML document.</summary>
public sealed record TenancyAgreementModel(
    string TenantName,
    string PropertyName,
    string UnitLabel,
    decimal RentAmount,
    DateTimeOffset? LeaseStartUtc,
    int? TermMonths,
    DateTimeOffset AcceptedAtUtc);

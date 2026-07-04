namespace TMG.Contracts.Events;

/// <summary>
/// Raised when a tenant accepts their tenancy and the agreement has been archived. The Consumer turns this
/// into an email letting the tenant know their signed agreement is available.
/// </summary>
public sealed record TenancyAgreementReady : BaseEvent
{
    public Guid TenancyId { get; init; }
    public Guid UnitId { get; init; }
    public Guid PropertyId { get; init; }
    public Guid AgreementDocumentId { get; init; }
    public string UnitLabel { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
}

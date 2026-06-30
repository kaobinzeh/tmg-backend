namespace TMG.Contracts.Events;

public sealed record TenantInvited : BaseEvent
{
    public string UnitLabel { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
}

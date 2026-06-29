namespace TMG.Application.Properties.Features.ListProperties;

public sealed record ListPropertiesResult(
    ListPropertiesStatus Status,
    IReadOnlyList<PropertyListItem> Properties);

public sealed record PropertyListItem(
    Guid Id,
    string Name,
    string Address,
    string? Description);

public enum ListPropertiesStatus
{
    Success = 1,
    NotAuthenticated = 2
}

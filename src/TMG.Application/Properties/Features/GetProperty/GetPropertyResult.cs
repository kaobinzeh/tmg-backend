namespace TMG.Application.Properties.Features.GetProperty;

public sealed record GetPropertyResult(
    GetPropertyStatus Status,
    PropertyDetail? Property);

public sealed record PropertyDetail(
    Guid Id,
    string Name,
    string Address,
    string? Description);

public enum GetPropertyStatus
{
    Success = 1,
    NotAuthenticated = 2,
    PropertyNotFound = 3
}

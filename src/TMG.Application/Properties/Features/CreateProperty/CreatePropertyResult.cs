namespace TMG.Application.Properties.Features.CreateProperty;

public sealed record CreatePropertyResult(
    CreatePropertyStatus Status,
    Guid? PropertyId = null);

public enum CreatePropertyStatus
{
    Success = 1,
    NotAuthenticated = 2
}

namespace TMG.Application.Properties.Features.AddUnit;

public sealed record AddUnitResult(
    AddUnitStatus Status,
    Guid? UnitId = null);

public enum AddUnitStatus
{
    Success = 1,
    NotAuthenticated = 2,
    PropertyNotFound = 3,
    DuplicateLabel = 4
}

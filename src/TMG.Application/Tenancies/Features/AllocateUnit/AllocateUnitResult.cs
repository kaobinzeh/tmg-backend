namespace TMG.Application.Tenancies.Features.AllocateUnit;

public sealed record AllocateUnitResult(
    AllocateUnitStatus Status,
    Guid? TenancyId = null);

public enum AllocateUnitStatus
{
    Success = 1,
    NotAuthenticated = 2,
    UnitNotFound = 3,
    UnitNotAvailable = 4,
    EmailRegisteredToAnotherClient = 5,
    TenantTypeNotConfigured = 6
}

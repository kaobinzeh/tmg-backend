namespace TMG.Application.Properties.Features.SetUnitAvailability;

public sealed record SetUnitAvailabilityResult(SetUnitAvailabilityStatus Status);

public enum SetUnitAvailabilityStatus
{
    Success = 1,
    NotAuthenticated = 2,
    UnitNotFound = 3
}

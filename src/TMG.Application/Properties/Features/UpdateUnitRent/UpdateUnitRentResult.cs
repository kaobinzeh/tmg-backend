namespace TMG.Application.Properties.Features.UpdateUnitRent;

public sealed record UpdateUnitRentResult(UpdateUnitRentStatus Status);

public enum UpdateUnitRentStatus
{
    Success = 1,
    NotAuthenticated = 2,
    UnitNotFound = 3
}

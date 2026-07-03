namespace TMG.Application.Tenancies.Features.ActivateTenancy;

public sealed record ActivateTenancyResult(ActivateTenancyStatus Status);

public enum ActivateTenancyStatus
{
    Success = 1,
    NotAuthenticated = 2,
    TenancyNotFound = 3,
    NotAccepted = 4,
    MissingLeaseTerms = 5
}

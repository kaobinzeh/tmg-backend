namespace TMG.Domain.Tenancies.ReadModels;

public sealed record TenancyRentSummaryReadModel(
    decimal CollectedAmount,
    decimal OutstandingAmount,
    int UpcomingRenewalsCount);

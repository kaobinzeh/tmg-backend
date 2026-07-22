using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace TMG.Infrastructure.Persistence;

public sealed class TenancyReadModelRepository(AppReadDbContext dbContext) : ITenancyReadModelRepository
{
    public async Task<IReadOnlyList<TenancyAllocationReadModel>> ListByClientAsync(
        Guid clientId,
        TenancyStatus? status,
        CancellationToken cancellationToken)
    {
        var tenancies = dbContext.Tenancies.AsNoTracking()
            .Where(tenancy => tenancy.ClientId == clientId);
        if (status is { } statusFilter)
        {
            tenancies = tenancies.Where(tenancy => tenancy.Status == statusFilter);
        }

        return await ProjectAllocationsAsync(
            tenancies.OrderByDescending(tenancy => tenancy.CreatedAtUtc),
            cancellationToken);
    }

    public async Task<IReadOnlyList<TenancyAllocationReadModel>> ListByTenantAsync(
        Guid clientId,
        Guid tenantStakeholderId,
        CancellationToken cancellationToken) =>
        await ProjectAllocationsAsync(
            dbContext.Tenancies.AsNoTracking()
                .Where(tenancy =>
                    tenancy.ClientId == clientId &&
                    tenancy.TenantStakeholderId == tenantStakeholderId &&
                    tenancy.Status != TenancyStatus.Rejected)
                .OrderByDescending(tenancy => tenancy.CreatedAtUtc),
            cancellationToken);

    public async Task<IReadOnlyList<TenancyAllocationReadModel>> ListUpcomingRenewalsAsync(
        Guid clientId,
        DateTimeOffset horizonUtc,
        CancellationToken cancellationToken) =>
        await ProjectAllocationsAsync(
            dbContext.Tenancies.AsNoTracking()
                .Where(tenancy =>
                    tenancy.ClientId == clientId &&
                    tenancy.Status == TenancyStatus.Active &&
                    tenancy.NextRentDueUtc != null &&
                    tenancy.NextRentDueUtc <= horizonUtc)
                .OrderBy(tenancy => tenancy.NextRentDueUtc),
            cancellationToken);

    public async Task<IReadOnlyList<UnitTenancyReadModel>> ListCurrentByPropertyAsync(
        Guid clientId,
        Guid propertyId,
        CancellationToken cancellationToken) =>
        await (
            from tenancy in dbContext.Tenancies.AsNoTracking()
            join stakeholderJoin in dbContext.Stakeholders.AsNoTracking()
                on tenancy.TenantStakeholderId equals stakeholderJoin.Id into stakeholders
            from stakeholder in stakeholders.DefaultIfEmpty()
            where tenancy.ClientId == clientId &&
                  tenancy.PropertyId == propertyId &&
                  (tenancy.Status == TenancyStatus.Invited ||
                   tenancy.Status == TenancyStatus.Accepted ||
                   tenancy.Status == TenancyStatus.Active)
            select new UnitTenancyReadModel(
                tenancy.UnitId,
                tenancy.Id,
                tenancy.Status,
                stakeholder == null ? null : stakeholder.FirstName + " " + stakeholder.LastName,
                stakeholder == null || stakeholder.AppUser.Email == null
                    ? tenancy.InvitedEmail
                    : stakeholder.AppUser.Email))
            .ToListAsync(cancellationToken);

    public async Task<TenancyRentSummaryReadModel> GetRentSummaryAsync(
        Guid clientId,
        DateTimeOffset nowUtc,
        DateTimeOffset renewalHorizonUtc,
        CancellationToken cancellationToken)
    {
        var collectedAmount = await dbContext.RentPayments.AsNoTracking()
            .Where(payment => payment.ClientId == clientId)
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;

        var outstandingAmount = await (
            from tenancy in dbContext.Tenancies.AsNoTracking()
            join unit in dbContext.Units.AsNoTracking() on tenancy.UnitId equals unit.Id
            where tenancy.ClientId == clientId &&
                  tenancy.Status == TenancyStatus.Active &&
                  tenancy.NextRentDueUtc != null &&
                  tenancy.NextRentDueUtc <= nowUtc
            select (decimal?)unit.RentAmount)
            .SumAsync(cancellationToken) ?? 0m;

        var upcomingRenewalsCount = await dbContext.Tenancies.AsNoTracking()
            .CountAsync(
                tenancy =>
                    tenancy.ClientId == clientId &&
                    tenancy.Status == TenancyStatus.Active &&
                    tenancy.NextRentDueUtc != null &&
                    tenancy.NextRentDueUtc <= renewalHorizonUtc,
                cancellationToken);

        return new TenancyRentSummaryReadModel(collectedAmount, outstandingAmount, upcomingRenewalsCount);
    }

    public async Task<IReadOnlyList<RentPaymentReadModel>> ListRentPaymentsByTenancyAsync(
        Guid clientId,
        Guid tenancyId,
        CancellationToken cancellationToken) =>
        await dbContext.RentPayments.AsNoTracking()
            .Where(payment => payment.ClientId == clientId && payment.TenancyId == tenancyId)
            .OrderByDescending(payment => payment.PaidAtUtc)
            .Select(payment => new RentPaymentReadModel(
                payment.Id,
                payment.Amount,
                payment.CurrencyId,
                payment.Method,
                payment.Reference,
                payment.PaidAtUtc,
                payment.PeriodStartUtc,
                payment.PeriodEndUtc,
                payment.ReceiptDocumentId))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<TenancyAllocationReadModel>> ProjectAllocationsAsync(
        IQueryable<Tenancy> tenancies,
        CancellationToken cancellationToken) =>
        await (
            from tenancy in tenancies
            join unit in dbContext.Units.AsNoTracking() on tenancy.UnitId equals unit.Id
            join property in dbContext.Properties.AsNoTracking() on tenancy.PropertyId equals property.Id
            join stakeholderJoin in dbContext.Stakeholders.AsNoTracking()
                on tenancy.TenantStakeholderId equals stakeholderJoin.Id into stakeholders
            from stakeholder in stakeholders.DefaultIfEmpty()
            select new TenancyAllocationReadModel(
                tenancy.Id,
                tenancy.UnitId,
                tenancy.PropertyId,
                tenancy.TenantStakeholderId,
                tenancy.Status,
                stakeholder == null ? null : stakeholder.FirstName + " " + stakeholder.LastName,
                stakeholder == null || stakeholder.AppUser.Email == null
                    ? tenancy.InvitedEmail
                    : stakeholder.AppUser.Email,
                unit.Label,
                property.Name,
                unit.RentAmount,
                unit.CurrencyId,
                tenancy.CycleStartUtc,
                tenancy.CycleEndUtc,
                tenancy.NextRentDueUtc))
            .ToListAsync(cancellationToken);
}

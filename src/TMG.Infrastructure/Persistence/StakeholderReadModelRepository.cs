using System.Linq.Expressions;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;

namespace TMG.Infrastructure.Persistence;

public sealed class StakeholderReadModelRepository(AppReadDbContext dbContext) : IStakeholderReadModelRepository
{
    public async Task<StakeholderReadModel?> GetByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default) =>
        await dbContext.Stakeholders
            .Where(stakeholder => stakeholder.AppUserId == appUserId)
            .Select(CreateReadModel())
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<StakeholderReadModel?> GetByStakeholderIdAsync(Guid stakeholderId, CancellationToken cancellationToken = default)
        => await dbContext.Stakeholders
            .Where(stakeholder => stakeholder.Id == stakeholderId)
            .Select(CreateReadModel())
            .FirstOrDefaultAsync(cancellationToken);

    private static Expression<Func<Stakeholder, StakeholderReadModel>> CreateReadModel() =>
        stakeholder => new StakeholderReadModel(
            stakeholder.Id,
            stakeholder.AppUserId,
            stakeholder.AppUser.Email ?? string.Empty,
            stakeholder.ClientId,
            stakeholder.CountryId,
            stakeholder.StakeholderTypeId,
            stakeholder.StakeholderType.Key,
            stakeholder.FirstName,
            stakeholder.LastName,
            stakeholder.AvatarUrl,
            stakeholder.IsVerified);
}

namespace TMG.Domain.Payments.ReadModels;

public interface IWalletReadModelRepository
{
    Task<IReadOnlyList<StakeholderWalletBalanceReadModel>> GetBalancesByStakeholderAsync(
        Guid stakeholderId,
        CancellationToken cancellationToken);
}

using TMG.Domain.Payments.ReadModels;

namespace TMG.Application.Payments.Features.GetStakeholderWalletBalances;

public sealed class GetStakeholderWalletBalancesHandler(IWalletReadModelRepository walletReadModelRepository)
{
    public async Task<GetStakeholderWalletBalancesResult> HandleAsync(
        GetStakeholderWalletBalancesCommand command,
        CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required to retrieve wallet balances.");

        var balances = await walletReadModelRepository.GetBalancesByStakeholderAsync(stakeholderId, cancellationToken);

        return new GetStakeholderWalletBalancesResult(
            balances
                .Select(balance => new GetStakeholderWalletBalancesResponse(
                    balance.WalletId,
                    balance.CurrencyId,
                    balance.CurrencyCode,
                    balance.CurrencySymbol,
                    balance.Balance))
                .ToArray());
    }
}

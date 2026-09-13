namespace TMG.Application.Payments.Features.GetStakeholderWalletBalances;

public sealed record GetStakeholderWalletBalancesResult(
    IReadOnlyList<GetStakeholderWalletBalancesResponse> Wallets);

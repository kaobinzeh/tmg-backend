namespace TMG.Application.Payments.Features.GetStakeholderWalletTransactions;

public sealed record GetStakeholderWalletTransactionsResult(
    IReadOnlyList<GetStakeholderWalletTransactionsResponse> Transactions,
    string? NextCursor);

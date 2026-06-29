namespace TMG.WebAPI.Features.Payments.WalletTransactions;

public sealed record GetStakeholderWalletTransactionsRequest(int Limit = 20, string? Cursor = null);

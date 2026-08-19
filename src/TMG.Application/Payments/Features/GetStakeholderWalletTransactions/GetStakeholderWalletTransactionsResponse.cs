namespace TMG.Application.Payments.Features.GetStakeholderWalletTransactions;

public sealed record GetStakeholderWalletTransactionsResponse(
    Guid WalletTransactionId,
    string TransactionTitle,
    decimal Amount,
    string CurrencyCode,
    string TransactionType,
    string TransactionCategory,
    DateTimeOffset Timestamp);

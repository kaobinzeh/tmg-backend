using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.ReadModels;

public sealed record StakeholderWalletTransactionReadModel(
    Guid WalletTransactionId,
    string TransactionTitle,
    decimal Amount,
    string CurrencyCode,
    WalletTransactionType TransactionType,
    WalletTransactionCategory TransactionCategory,
    DateTimeOffset Timestamp);

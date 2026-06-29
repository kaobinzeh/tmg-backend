using TMG.Contracts.Payments;

namespace TMG.Domain.Payments.ReadModels;

public sealed record StakeholderWalletTopUpTransactionDetailReadModel(
    Guid WalletTransactionId,
    string TransactionTitle,
    string? Description,
    string MerchantReference,
    decimal Amount,
    string CurrencyCode,
    PaymentMethodType PaymentMethodType,
    string PaymentProviderName,
    DateTimeOffset Timestamp);

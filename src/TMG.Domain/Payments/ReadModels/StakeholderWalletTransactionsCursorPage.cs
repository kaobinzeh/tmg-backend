namespace TMG.Domain.Payments.ReadModels;

public sealed record StakeholderWalletTransactionsCursorPage(
    IReadOnlyList<StakeholderWalletTransactionReadModel> Transactions,
    bool HasMore);

namespace TMG.Domain.Payments.ReadModels;

/// <summary>
/// A stakeholder's balance in a single currency. <see cref="WalletId"/> is null when the stakeholder
/// holds no wallet in that currency yet — wallets are created lazily on the first successful top-up,
/// so a zero balance is reported against their country's default currency rather than no balance at all.
/// </summary>
public sealed record StakeholderWalletBalanceReadModel(
    Guid? WalletId,
    Guid CurrencyId,
    string CurrencyCode,
    string? CurrencySymbol,
    decimal Balance);

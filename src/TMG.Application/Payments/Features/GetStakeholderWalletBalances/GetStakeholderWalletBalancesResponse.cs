namespace TMG.Application.Payments.Features.GetStakeholderWalletBalances;

/// <summary>
/// A balance in a single currency. <see cref="WalletId"/> is null when no wallet has been created yet,
/// which is the case until the stakeholder's first successful top-up; the balance is zero in that case.
/// </summary>
public sealed record GetStakeholderWalletBalancesResponse(
    Guid? WalletId,
    Guid CurrencyId,
    string CurrencyCode,
    string? CurrencySymbol,
    decimal Balance);

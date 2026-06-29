using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class WalletByStakeholderAndCurrencySpecification : Specification<Wallet>
{
    public WalletByStakeholderAndCurrencySpecification(Guid stakeholderId, Guid currencyId)
    {
        Where(wallet => wallet.StakeholderId == stakeholderId && wallet.CurrencyId == currencyId);
        EnableTracking();
    }
}

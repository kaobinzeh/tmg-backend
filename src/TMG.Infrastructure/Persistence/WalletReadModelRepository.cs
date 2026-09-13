using TMG.Domain.Payments.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace TMG.Infrastructure.Persistence;

public sealed class WalletReadModelRepository(AppReadDbContext dbContext) : IWalletReadModelRepository
{
    public async Task<IReadOnlyList<StakeholderWalletBalanceReadModel>> GetBalancesByStakeholderAsync(
        Guid stakeholderId,
        CancellationToken cancellationToken)
    {
        var balances = await (
            from wallet in dbContext.Wallets.AsNoTracking()
            join currency in dbContext.Currencies.AsNoTracking()
                on wallet.CurrencyId equals currency.Id
            where wallet.StakeholderId == stakeholderId
            orderby currency.CurrencyCode
            select new StakeholderWalletBalanceReadModel(
                wallet.Id,
                currency.Id,
                currency.CurrencyCode,
                currency.CurrencySymbol,
                wallet.Balance))
            .ToListAsync(cancellationToken);

        if (balances.Count > 0)
        {
            return balances;
        }

        // No wallet row exists until the first successful top-up credits one, so report a zero
        // balance in the stakeholder's country default currency instead of an empty ledger.
        var defaultCurrency = await (
            from stakeholder in dbContext.Stakeholders.AsNoTracking()
            join countryCurrency in dbContext.CountryCurrencies.AsNoTracking()
                on stakeholder.CountryId equals countryCurrency.CountryId
            join currency in dbContext.Currencies.AsNoTracking()
                on countryCurrency.CurrencyId equals currency.Id
            where stakeholder.Id == stakeholderId
                && countryCurrency.IsDefault
                && countryCurrency.IsActive
            select new StakeholderWalletBalanceReadModel(
                null,
                currency.Id,
                currency.CurrencyCode,
                currency.CurrencySymbol,
                0m))
            .FirstOrDefaultAsync(cancellationToken);

        return defaultCurrency is null ? [] : [defaultCurrency];
    }
}

using TMG.Domain.Common.Entities;
using TMG.Domain.Common.Exceptions;

namespace TMG.Domain.Payments.Entities;

public sealed class Wallet : Entity, IAggregateRoot
{
    private Wallet()
    {
    }

    private Wallet(Guid stakeholderId, Guid clientId, Guid currencyId, decimal balance)
    {
        StakeholderId = stakeholderId;
        ClientId = clientId;
        CurrencyId = currencyId;
        Balance = balance;
    }

    public Guid StakeholderId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid CurrencyId { get; private set; }
    public decimal Balance { get; private set; }
    public uint RowVersion { get; private set; }

    public static Wallet Create(Guid stakeholderId, Guid clientId, Guid currencyId) =>
        new(stakeholderId, clientId, currencyId, 0m);

    public void Credit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new AggregateStateException("Wallet credit amount must be greater than zero.");
        }

        Balance += amount;
    }
}

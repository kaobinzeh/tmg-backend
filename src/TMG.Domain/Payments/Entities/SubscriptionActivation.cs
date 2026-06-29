using TMG.Domain.Common.Entities;

namespace TMG.Domain.Payments.Entities;

public sealed class SubscriptionActivation : Entity, IAggregateRoot
{
    private SubscriptionActivation()
    {
    }

    private SubscriptionActivation(
        Guid paymentTransactionId,
        Guid stakeholderId,
        Guid clientId,
        string? subscriptionReference,
        decimal amount,
        Guid currencyId,
        DateTimeOffset activatedAtUtc)
    {
        PaymentTransactionId = paymentTransactionId;
        StakeholderId = stakeholderId;
        ClientId = clientId;
        SubscriptionReference = string.IsNullOrWhiteSpace(subscriptionReference) ? null : subscriptionReference.Trim();
        Amount = amount;
        CurrencyId = currencyId;
        ActivatedAtUtc = activatedAtUtc;
    }

    public Guid PaymentTransactionId { get; private set; }
    public Guid StakeholderId { get; private set; }
    public Guid ClientId { get; private set; }
    public string? SubscriptionReference { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CurrencyId { get; private set; }
    public DateTimeOffset ActivatedAtUtc { get; private set; }

    public static SubscriptionActivation Create(
        Guid paymentTransactionId,
        Guid stakeholderId,
        Guid clientId,
        string? subscriptionReference,
        decimal amount,
        Guid currencyId,
        DateTimeOffset activatedAtUtc) =>
        new(paymentTransactionId, stakeholderId, clientId, subscriptionReference, amount, currencyId, activatedAtUtc);
}

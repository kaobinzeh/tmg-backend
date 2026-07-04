using TMG.Contracts.Payments;

namespace TMG.Contracts.Events;

public sealed record SuccessfulPaymentConfirmed : BaseEvent
{
    public Guid PaymentTransactionId { get; init; }
    public string MerchantReference { get; init; } = string.Empty;
    public PaymentIntent PaymentIntent { get; init; }
    public Guid PaymentProviderId { get; init; }
    public decimal Amount { get; init; }
    public Guid CurrencyId { get; init; }

    /// <summary>Set when <see cref="PaymentIntent"/> is <see cref="PaymentIntent.RentPayment"/>; identifies the tenancy to settle.</summary>
    public Guid? TenancyId { get; init; }
}

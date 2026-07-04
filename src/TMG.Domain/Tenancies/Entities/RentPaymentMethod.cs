namespace TMG.Domain.Tenancies.Entities;

public enum RentPaymentMethod
{
    BankTransfer = 1,
    Cash = 2,
    Cheque = 3,
    Card = 4,
    Other = 5,

    /// <summary>Paid in-app through a payment gateway (Credo/SafeHaven).</summary>
    Online = 6
}

namespace TMG.Domain.Common.Notifications;

/// <summary>Renders a rent-payment receipt as a self-contained HTML document for archival + delivery.</summary>
public interface IRentReceiptRenderer
{
    string Render(RentReceiptModel model);
}

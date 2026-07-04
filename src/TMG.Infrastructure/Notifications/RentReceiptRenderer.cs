using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using TMG.Domain.Common.Notifications;

namespace TMG.Infrastructure.Notifications;

/// <summary>
/// Renders a rent-payment receipt as a self-contained HTML document. Placeholders in the template use the
/// <c>{{:Key:}}</c> convention, mirroring <see cref="NoticeLetterRenderer"/> and the email templates.
/// </summary>
internal sealed partial class RentReceiptRenderer : IRentReceiptRenderer
{
    private const string Template = """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>Rent Payment Receipt</title>
        </head>
        <body style="font-family:Georgia,'Times New Roman',serif;color:#1a1a1a;line-height:1.6;max-width:720px;margin:0 auto;padding:32px;">
          <h1 style="font-size:20px;text-transform:uppercase;letter-spacing:1px;border-bottom:2px solid #1a1a1a;padding-bottom:8px;">Rent Payment Receipt</h1>
          <p>Receipt no.: <strong>{{:ReceiptNumber:}}</strong></p>
          <p>Date of payment: <strong>{{:PaidDate:}}</strong></p>
          <p>Received from <strong>{{:TenantName:}}</strong>, the sum of <strong>{{:Amount:}}</strong>, being rent
          for <strong>{{:UnitLabel:}}</strong> at <strong>{{:PropertyName:}}</strong>.</p>
          <table style="width:100%;border-collapse:collapse;margin-top:16px;">
            <tr>
              <td style="padding:6px 0;color:#555;">Period covered</td>
              <td style="padding:6px 0;text-align:right;"><strong>{{:PeriodStart:}} &ndash; {{:PeriodEnd:}}</strong></td>
            </tr>
            <tr>
              <td style="padding:6px 0;color:#555;">Payment method</td>
              <td style="padding:6px 0;text-align:right;"><strong>{{:PaymentMethod:}}</strong></td>
            </tr>
            <tr>
              <td style="padding:6px 0;color:#555;">Reference</td>
              <td style="padding:6px 0;text-align:right;"><strong>{{:Reference:}}</strong></td>
            </tr>
          </table>
          <p style="margin-top:24px;">This receipt confirms payment has been received in full for the period stated above.</p>
          <p>Yours faithfully,<br />The Property Management Team</p>
          <p style="font-size:12px;color:#777;margin-top:32px;border-top:1px solid #ddd;padding-top:12px;">
            This is an automatically generated receipt retained in your tenancy records.
          </p>
        </body>
        </html>
        """;

    public string Render(RentReceiptModel model)
    {
        var values = new Dictionary<string, string>
        {
            ["ReceiptNumber"] = model.ReceiptNumber,
            ["PaidDate"] = FormatDate(model.PaidAtUtc),
            ["TenantName"] = model.TenantName,
            ["Amount"] = model.Amount.ToString("N2", CultureInfo.InvariantCulture),
            ["UnitLabel"] = model.UnitLabel,
            ["PropertyName"] = model.PropertyName,
            ["PeriodStart"] = FormatDate(model.PeriodStartUtc),
            ["PeriodEnd"] = FormatDate(model.PeriodEndUtc),
            ["PaymentMethod"] = model.PaymentMethod,
            ["Reference"] = string.IsNullOrWhiteSpace(model.Reference) ? "—" : model.Reference
        };

        return PlaceholderPattern().Replace(Template, match =>
        {
            var key = match.Groups["key"].Value;
            return values.TryGetValue(key, out var value) ? WebUtility.HtmlEncode(value) : match.Value;
        });
    }

    private static string FormatDate(DateTimeOffset value) =>
        value.UtcDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"\{\{:(?<key>[A-Za-z0-9_]+):\}\}")]
    private static partial Regex PlaceholderPattern();
}

using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using TMG.Domain.Common.Notifications;

namespace TMG.Infrastructure.Notifications;

/// <summary>
/// Renders the tenancy agreement as a self-contained HTML document. Placeholders use the <c>{{:Key:}}</c>
/// convention, mirroring <see cref="NoticeLetterRenderer"/> and <see cref="RentReceiptRenderer"/>.
/// </summary>
internal sealed partial class TenancyAgreementRenderer : ITenancyAgreementRenderer
{
    private const string Template = """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>Tenancy Agreement</title>
        </head>
        <body style="font-family:Georgia,'Times New Roman',serif;color:#1a1a1a;line-height:1.6;max-width:720px;margin:0 auto;padding:32px;">
          <h1 style="font-size:20px;text-transform:uppercase;letter-spacing:1px;border-bottom:2px solid #1a1a1a;padding-bottom:8px;">Tenancy Agreement</h1>
          <p>This agreement is entered into on <strong>{{:AcceptedDate:}}</strong> between the property management
          and <strong>{{:TenantName:}}</strong> (the "Tenant").</p>
          <table style="width:100%;border-collapse:collapse;margin-top:16px;">
            <tr>
              <td style="padding:6px 0;color:#555;">Property</td>
              <td style="padding:6px 0;text-align:right;"><strong>{{:PropertyName:}}</strong></td>
            </tr>
            <tr>
              <td style="padding:6px 0;color:#555;">Unit</td>
              <td style="padding:6px 0;text-align:right;"><strong>{{:UnitLabel:}}</strong></td>
            </tr>
            <tr>
              <td style="padding:6px 0;color:#555;">Rent per term</td>
              <td style="padding:6px 0;text-align:right;"><strong>{{:RentAmount:}}</strong></td>
            </tr>
            <tr>
              <td style="padding:6px 0;color:#555;">Lease start</td>
              <td style="padding:6px 0;text-align:right;"><strong>{{:LeaseStart:}}</strong></td>
            </tr>
            <tr>
              <td style="padding:6px 0;color:#555;">Term</td>
              <td style="padding:6px 0;text-align:right;"><strong>{{:Term:}}</strong></td>
            </tr>
          </table>
          <p style="margin-top:24px;">The Tenant has accepted the terms and conditions of this tenancy and agrees to pay
          the rent stated above for each term of occupancy, and to keep the premises in good condition.</p>
          <p>Accepted by the Tenant on <strong>{{:AcceptedDate:}}</strong>.</p>
          <p style="font-size:12px;color:#777;margin-top:32px;border-top:1px solid #ddd;padding-top:12px;">
            This is an automatically generated agreement retained in your tenancy records.
          </p>
        </body>
        </html>
        """;

    public string Render(TenancyAgreementModel model)
    {
        var values = new Dictionary<string, string>
        {
            ["AcceptedDate"] = FormatDate(model.AcceptedAtUtc),
            ["TenantName"] = model.TenantName,
            ["PropertyName"] = model.PropertyName,
            ["UnitLabel"] = model.UnitLabel,
            ["RentAmount"] = model.RentAmount.ToString("N2", CultureInfo.InvariantCulture),
            ["LeaseStart"] = model.LeaseStartUtc is { } start ? FormatDate(start) : "To be confirmed",
            ["Term"] = model.TermMonths is { } months ? $"{months} month(s)" : "To be confirmed"
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

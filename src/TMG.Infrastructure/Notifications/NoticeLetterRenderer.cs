using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using TMG.Domain.Common.Notifications;

namespace TMG.Infrastructure.Notifications;

/// <summary>
/// Renders a formal rent-due notice letter as a self-contained HTML document. Placeholders in the template
/// use the <c>{{:Key:}}</c> convention, mirroring the email templates.
/// </summary>
internal sealed partial class NoticeLetterRenderer : INoticeLetterRenderer
{
    private const string Template = """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>Rent Renewal Notice</title>
        </head>
        <body style="font-family:Georgia,'Times New Roman',serif;color:#1a1a1a;line-height:1.6;max-width:720px;margin:0 auto;padding:32px;">
          <h1 style="font-size:20px;text-transform:uppercase;letter-spacing:1px;border-bottom:2px solid #1a1a1a;padding-bottom:8px;">Notice of Rent Renewal</h1>
          <p>Date of notice: <strong>{{:NoticeDate:}}</strong></p>
          <p>Dear {{:TenantName:}},</p>
          <p>
            This letter serves as formal notice, given <strong>{{:NoticePeriodLabel:}}</strong> in advance, that the rent
            for <strong>{{:UnitLabel:}}</strong> at <strong>{{:PropertyName:}}</strong> is due for renewal on
            <strong>{{:NextRentDueDate:}}</strong>.
          </p>
          <p>
            The rent payable for the forthcoming term is <strong>{{:RentAmount:}}</strong>. Kindly ensure payment is made
            on or before the due date to maintain your tenancy in good standing and avoid any interruption to your occupancy.
          </p>
          <p>
            Should you have already settled this amount, or if you intend not to renew, please disregard this notice and
            contact your property manager at your earliest convenience.
          </p>
          <p>Yours faithfully,<br />The Property Management Team</p>
          <p style="font-size:12px;color:#777;margin-top:32px;border-top:1px solid #ddd;padding-top:12px;">
            This is an automatically generated notice retained in your tenancy records.
          </p>
        </body>
        </html>
        """;

    public string Render(NoticeLetterModel model)
    {
        var values = new Dictionary<string, string>
        {
            ["NoticeDate"] = FormatDate(model.NextRentDueUtc.Subtract(NoticeLead(model.NoticePeriodLabel))),
            ["TenantName"] = model.TenantName,
            ["NoticePeriodLabel"] = model.NoticePeriodLabel,
            ["UnitLabel"] = model.UnitLabel,
            ["PropertyName"] = model.PropertyName,
            ["NextRentDueDate"] = FormatDate(model.NextRentDueUtc),
            ["RentAmount"] = model.RentAmount.ToString("N2", CultureInfo.InvariantCulture)
        };

        return PlaceholderPattern().Replace(Template, match =>
        {
            var key = match.Groups["key"].Value;
            return values.TryGetValue(key, out var value) ? WebUtility.HtmlEncode(value) : match.Value;
        });
    }

    private static string FormatDate(DateTimeOffset value) =>
        value.UtcDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

    private static TimeSpan NoticeLead(string noticePeriodLabel) =>
        noticePeriodLabel.StartsWith("1", StringComparison.Ordinal) ? TimeSpan.FromDays(30) : TimeSpan.FromDays(90);

    [GeneratedRegex(@"\{\{:(?<key>[A-Za-z0-9_]+):\}\}")]
    private static partial Regex PlaceholderPattern();
}

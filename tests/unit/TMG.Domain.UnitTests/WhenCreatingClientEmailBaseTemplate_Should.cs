using TMG.Domain.Notifications.Entities;

namespace TMG.Domain.UnitTests;

public sealed class WhenCreatingClientEmailBaseTemplate_Should
{
    [Fact]
    public void SetFieldsAndAuditDates()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var clientId = Guid.CreateVersion7();

        var template = ClientEmailBaseTemplate.Create(clientId, " Primary tenant brand ", " <html><body>{{:BodyHtml:}}</body></html> ");

        template.ClientId.ShouldBe(clientId);
        template.Description.ShouldBe("Primary tenant brand");
        template.HtmlTemplate.ShouldBe("<html><body>{{:BodyHtml:}}</body></html>");
        template.CreatedAtUtc.ShouldBe(default);
        template.UpdatedAtUtc.ShouldBe(default);
    }
}



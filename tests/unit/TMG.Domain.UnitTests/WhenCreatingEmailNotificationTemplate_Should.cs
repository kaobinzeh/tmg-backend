using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Notifications.Entities;

namespace TMG.Domain.UnitTests;

public sealed class WhenCreatingEmailNotificationTemplate_Should
{
    [Fact]
    public void SetFieldsAndAuditDates()
    {
        var utcNow = DateTimeOffset.UtcNow;

        var template = EmailNotificationTemplate.Create(NotificationType.SignInSuccessful, " Sign-in successful notification ", " Successful sign-in ", " SignInSuccessful.html ");

        template.NotificationType.ShouldBe(NotificationType.SignInSuccessful);
        template.Description.ShouldBe("Sign-in successful notification");
        template.Subject.ShouldBe("Successful sign-in");
        template.TemplateFileName.ShouldBe("SignInSuccessful.html");
        template.CreatedAtUtc.ShouldBe(default);
        template.UpdatedAtUtc.ShouldBe(default);
    }
}



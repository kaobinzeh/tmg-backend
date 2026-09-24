using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Formatting;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Authentication;

public enum EmailConfirmationOtpSendOutcome
{
    Sent,
    AlreadyConfirmed,
    OtpAlreadyActive
}

/// <summary>
/// Generates the cached sign-up OTP and queues the confirmation email. Shared by the
/// <see cref="UserCreatedHandler"/> (first send) and the <see cref="SendEmailConfirmationOtpHandler"/> (resend).
/// </summary>
public sealed class EmailConfirmationOtpSender(
    IAuthenticationIdentityService identityService,
    ITwoFactorOtpService twoFactorOtpService,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<EmailConfirmationOtpSendOutcome> SendAsync(
        StakeholderReadModel stakeholder,
        bool replaceActiveOtp,
        CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(stakeholder.AppUserId)
            ?? throw new CannotProcessMessageNonTransientException(
                $"Unable to send the email confirmation OTP because no user could be found for stakeholder '{stakeholder.StakeholderId}'.");

        if (user.EmailConfirmed)
        {
            return EmailConfirmationOtpSendOutcome.AlreadyConfirmed;
        }

        if (!replaceActiveOtp
            && await twoFactorOtpService.OtpExistsAsync(stakeholder.AppUserId, OtpIntent.EmailConfirmation, cancellationToken))
        {
            return EmailConfirmationOtpSendOutcome.OtpAlreadyActive;
        }

        var otp = await twoFactorOtpService.GenerateOtpAsync(
            stakeholder.AppUserId,
            OtpIntent.EmailConfirmation,
            cancellationToken,
            characterLength: 6,
            isAlphaNumeric: false);

        await commandSender.SendAsync(
            new SendNotificationCommand(
                stakeholder.ClientId,
                stakeholder.CountryId,
                NotificationType.EmailConfirmationOtp,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = stakeholder.FirstName,
                        ["LastName"] = stakeholder.LastName,
                        ["OtpCode"] = otp.Code,
                        ["OtpExpiresAtUtc"] = DateTimeFormatter.FormatHumanReadableUtc(otp.ExpiresAtUtc, timeProvider.GetUtcNow()),
                        ["VerifyUrl"] = string.Empty,
                        ["Product"] = "TMG"
                    }))
            {
                StakeholderId = stakeholder.StakeholderId
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EmailConfirmationOtpSendOutcome.Sent;
    }
}

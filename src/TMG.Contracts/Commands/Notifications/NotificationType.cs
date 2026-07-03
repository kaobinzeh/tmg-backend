namespace TMG.Contracts.Commands.Notifications;

public enum NotificationType
{
    AccountCreated = 1,
    EmailConfirmationOtp = 2,
    ResetPasswordOtp = 3,
    PasswordResetSuccessful = 4,
    EmailConfirmationFollowUp = 5,
    SignInSuccessful = 6,
    AccountLocked = 7,
    TrialExpired = 8,
    SubscriptionCancelled = 9,
    SubscriptionInvoice = 10,
    UnitAllocationInvitation = 11,
    RentDueReminder3Months = 12,
    RentDueReminder1Month = 13
}

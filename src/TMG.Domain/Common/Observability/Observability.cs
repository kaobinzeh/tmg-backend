namespace TMG.Domain.Common.Observability;

public static class Observability
{
    public const string ActivitySourceName = "TMG";
    public const string FlowIdHeaderName = "X-Flow-Id";

    public static class PropertyNames
    {
        public static class Common
        {
            public const string MessageType = "message_type";
            public const string MessageId = "message_id";
            public const string UserId = "user_id";
            public const string StakeholderId = "stakeholder_id";
            public const string ClientId = "tenant_id";
            public const string CorrelationId = "correlation_id";
            public const string FlowId = "flow.id";
            public const string FailureReason = "failure_reason";
            public const string ExceptionType = "exception_type";
        }

        public static class Notifications
        {
            public const string ProviderKey = "provider_key";
            public const string ProviderMessageId = "provider_message_id";
            public const string NotificationType = "notification_type";
        }

        public static class Payments
        {
            public const string Provider = "provider";
            public const string PaymentReference = "payment_reference";
            public const string MerchantReference = "merchant_reference";
            public const string ProviderReference = "provider_reference";
            public const string PaymentMethod = "payment_method";
            public const string PaymentIntent = "payment_intent";
            public const string CurrencyId = "currency_id";
            public const string CurrencyCode = "currency_code";
            public const string WalletId = "wallet_id";
            public const string TerminalState = "terminal_state";
        }
    }

    public static class EventNames
    {
        public static class Authentication
        {
            public const string PasswordSignUpStarted = "PasswordSignUpStarted";
            public const string PasswordSignUpCompleted = "PasswordSignUpCompleted";
            public const string PasswordSignUpFailed = "PasswordSignUpFailed";
            public const string GoogleSignUpStarted = "GoogleSignUpStarted";
            public const string GoogleSignUpCompleted = "GoogleSignUpCompleted";
            public const string GoogleSignUpFailed = "GoogleSignUpFailed";
            public const string EmailConfirmationOtpSent = "EmailConfirmationOtpSent";
            public const string EmailConfirmationOtpResendRequested = "EmailConfirmationOtpResendRequested";
            public const string EmailConfirmationOtpResendFailed = "EmailConfirmationOtpResendFailed";
            public const string EmailConfirmationStarted = "EmailConfirmationStarted";
            public const string EmailConfirmationCompleted = "EmailConfirmationCompleted";
            public const string EmailConfirmationFailed = "EmailConfirmationFailed";
            public const string PasswordSignInStarted = "PasswordSignInStarted";
            public const string PasswordSignInCompleted = "PasswordSignInCompleted";
            public const string GoogleSignInStarted = "GoogleSignInStarted";
            public const string GoogleSignInCompleted = "GoogleSignInCompleted";
            public const string SignInPostProcessingCompleted = "SignInPostProcessingCompleted";
            public const string SignInFailureProcessed = "SignInFailureProcessed";
            public const string PasswordResetRequested = "PasswordResetRequested";
            public const string PasswordResetRequestFailed = "PasswordResetRequestFailed";
            public const string PasswordResetOtpSent = "PasswordResetOtpSent";
            public const string PasswordResetCompleted = "PasswordResetCompleted";
            public const string PasswordResetCompletionFailed = "PasswordResetCompletionFailed";
            public const string SignOutCompleted = "SignOutCompleted";
            public const string SessionRefreshCompleted = "SessionRefreshCompleted";
            public const string SessionRefreshPostProcessingCompleted = "SessionRefreshPostProcessingCompleted";
            public const string ProfileUpdateCompleted = "ProfileUpdateCompleted";
            public const string ProfileUpdateFailed = "ProfileUpdateFailed";
            public const string AvatarUploadCompleted = "AvatarUploadCompleted";
            public const string AvatarUploadFailed = "AvatarUploadFailed";
        }

        public static class Notifications
        {
            public const string EmailSent = "EmailNotificationSent";
            public const string EmailDelivered = "EmailNotificationDelivered";
        }

        public static class Payments
        {
            public const string Initiated = "payment.initiated";
            public const string WebhookReceived = "payment.webhook.received";
            public const string WebhookPersisted = "payment.webhook.persisted";
            public const string WebhookPersistenceFailed = "payment.webhook.persistence_failed";
            public const string ReconciliationConfirmed = "payment.reconciliation.confirmed";
            public const string ReconciliationFailed = "payment.reconciliation.failed";
            public const string SubscriptionActivated = "payment.subscription.activated";
            public const string WalletCreated = "payment.wallet.created";
            public const string WalletCredited = "payment.wallet.credited";
            public const string CreditWallet = "payment.credit_wallet";
            public const string ActivateSubscription = "payment.activate_subscription";
            public const string RecordRentPayment = "payment.record_rent_payment";
            public const string RentPaymentRecorded = "payment.rent_payment.recorded";
        }
    }
}

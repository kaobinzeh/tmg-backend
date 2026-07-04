namespace TMG.WebAPI;

public static class EndpointUrl
{
    public static class Versions
    {
        public const string V1 = "v1";
        public const string V1Route = "v{version:apiVersion}";
    }

    public static class Registrations
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/registrations";
        public static readonly string V1 = ToV1(Route);
    }

    public static class EmailConfirmations
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/email-confirmations";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Sessions
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/sessions";
        public static readonly string V1 = ToV1(Route);
        public static readonly string GoogleV1 = $"{V1}/google";
        public static readonly string RefreshV1 = $"{V1}/refresh";
        public static readonly string LogoutV1 = $"{V1}/logout";
    }

    public static class GoogleRegistrations
    {
        public const string Route = $"{Registrations.Route}/google";
        public static readonly string V1 = ToV1(Route);
    }

    public static class PasswordResets
    {
        public const string Route = $"api/{Versions.V1Route}/authentication/password-resets";
        public static readonly string V1 = ToV1(Route);
        public static readonly string CompletionsV1 = $"{V1}/completions";
    }

    public static class Countries
    {
        public const string Route = $"api/{Versions.V1Route}/reference-data/countries";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Stakeholders
    {
        public const string Route = $"api/{Versions.V1Route}/stakeholders";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Providers
    {
        public const string Route = $"api/{Versions.V1Route}/providers";
        public static readonly string V1 = ToV1(Route);
    }

    public static class Properties
    {
        public const string Route = $"api/{Versions.V1Route}/properties";
        public static readonly string V1 = ToV1(Route);
        public static string UnitsV1(Guid propertyId) => $"{V1}/{propertyId}/units";
    }

    public static class Units
    {
        public const string Route = $"api/{Versions.V1Route}/units";
        public static readonly string V1 = ToV1(Route);
        public static string RentV1(Guid unitId) => $"{V1}/{unitId}/rent";
        public static string AvailabilityV1(Guid unitId) => $"{V1}/{unitId}/availability";
    }

    public static class Tenancies
    {
        public const string Route = $"api/{Versions.V1Route}/tenancies";
        public static readonly string V1 = ToV1(Route);
        public static readonly string AllocationsV1 = $"{V1}/allocations";
        public static readonly string AcceptInvitationV1 = $"{V1}/accept-invitation";
        public static readonly string RejectInvitationV1 = $"{V1}/reject-invitation";
        public static readonly string DocumentsV1 = $"{V1}/documents";
        public static readonly string UpcomingRenewalsV1 = $"{V1}/allocations/upcoming-renewals";
        public static string ActivateV1(Guid tenancyId) => $"{V1}/allocations/{tenancyId}/activate";
        public static string CycleV1(Guid tenancyId) => $"{V1}/allocations/{tenancyId}/cycle";
        public static string PaymentsV1(Guid tenancyId) => $"{V1}/allocations/{tenancyId}/payments";
        public static string DocumentsForTenancyV1(Guid tenancyId) => $"{V1}/allocations/{tenancyId}/documents";
        public static string DocumentDownloadUrlV1(Guid documentId) => $"{V1}/documents/{documentId}/download-url";
    }

    public static class Payments
    {
        public const string Route = $"api/{Versions.V1Route}/payments";
        public static readonly string V1 = ToV1(Route);
        public static readonly string InitiateV1 = $"{V1}/initiate";
        public static readonly string WalletTransactionsV1 = $"{V1}/wallet-transactions";
        public static string WalletTopUpTransactionDetailsV1(Guid walletTransactionId) =>
            $"{WalletTransactionsV1}/top-ups/{walletTransactionId}";
    }

    public static class PaymentProviders
    {
        public const string Route = $"api/{Versions.V1Route}/payment-providers";
        public static readonly string V1 = ToV1(Route);
    }

    public static class PaymentWebhooks
    {
        public static class SafeHaven
        {
            public const string Route = $"api/{Versions.V1Route}/payments/webhooks/safehaven";
            public static readonly string V1 = ToV1(Route);
        }

        public static class Credo
        {
            public const string Route = $"api/{Versions.V1Route}/payments/webhooks/credo";
            public static readonly string V1 = ToV1(Route);
        }
    }

    public static class EmailNotificationWebhooks
    {
        public static class Mailtrap
        {
            public const string Route = $"api/{Versions.V1Route}/email-notifications/webhooks/mailtrap";
            public static readonly string V1 = ToV1(Route);
        }
    }

    private static string ToV1(string routeTemplate) =>
        routeTemplate.Replace(Versions.V1Route, Versions.V1, StringComparison.Ordinal);
}

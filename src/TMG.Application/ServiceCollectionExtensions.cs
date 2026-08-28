using TMG.Application.Authentication.Features.CompletePasswordReset;
using TMG.Application.Authentication.Features.GoogleSignIn;
using TMG.Application.Authentication.Features.GoogleSignUp;
using TMG.Application.Authentication.Features.LogoutSession;
using TMG.Application.Authentication.Features.RefreshSession;
using TMG.Application.Authentication.Features.RequestPasswordReset;
using TMG.Application.Authentication.Features.SignIn;
using TMG.Application.Authentication.Features.SignUp;
using TMG.Application.Authentication.Features.SignUpOtp;
using TMG.Application.Authentication.Stakeholders;
using TMG.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;
using TMG.Application.Payments.Features.ActivatePaymentProvider;
using TMG.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using TMG.Application.Payments.Features.GetStakeholderWalletTransactions;
using TMG.Application.Payments.Features.GetPaymentProviders;
using TMG.Application.Payments.Features.InitiatePayment;
using TMG.Application.Payments.Features.ProcessCredoWebhook;
using TMG.Application.Payments.Features.ProcessSafeHavenWebhook;
using TMG.Application.Payments.Features.ReconcilePayments;
using TMG.Application.Properties.Features.AddUnit;
using TMG.Application.Properties.Features.CreateProperty;
using TMG.Application.Properties.Features.ListProperties;
using TMG.Application.Properties.Features.ListUnits;
using TMG.Application.Properties.Features.SetUnitAvailability;
using TMG.Application.Properties.Features.UpdateUnitRent;
using TMG.Application.Providers.Features.ActivateProvider;
using TMG.Application.ReferenceData.Features.GetCountries;
using TMG.Application.ReferenceData.Features.GetCurrencies;
using TMG.Application.Tenancies.Features.AcceptTenancyInvitation;
using TMG.Application.Tenancies.Features.ActivateTenancy;
using TMG.Application.Tenancies.Features.AllocateUnit;
using TMG.Application.Tenancies;
using TMG.Application.Tenancies.Features.GetTenancyCycle;
using TMG.Application.Tenancies.Features.GetTenancyDocumentDownloadUrl;
using TMG.Application.Tenancies.Features.GetTenancySummary;
using TMG.Application.Tenancies.Features.ListMyTenancies;
using TMG.Application.Tenancies.Features.ListTenancyAllocations;
using TMG.Application.Tenancies.Features.ListTenancyDocuments;
using TMG.Application.Tenancies.Features.ListTenancyRentPayments;
using TMG.Application.Tenancies.Features.ListUpcomingRenewals;
using TMG.Application.Tenancies.Features.ProcessRentReminders;
using TMG.Application.Tenancies.Features.RecordRentPayment;
using TMG.Application.Tenancies.Features.RejectTenancyInvitation;
using TMG.Application.Tenancies.Features.UploadTenancyDocument;
using TMG.Application.Stakeholders.Features.GetMyProfile;
using TMG.Application.Stakeholders.Features.UpdateProfile;
using TMG.Application.Stakeholders.Features.UploadAvatar;
using TMG.Application.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TMG.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var onboardingSection = configuration.GetSection(ClientOnboardingOptions.SectionName);

        // Sign-up resolves this lazily, so an unset value would surface as a 500 on the first
        // registration rather than a failed deployment.
        var defaultClientId = onboardingSection.Get<ClientOnboardingOptions>()?.DefaultClientId;
        if (defaultClientId is null || defaultClientId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"'{ClientOnboardingOptions.SectionName}:{nameof(ClientOnboardingOptions.DefaultClientId)}' must be configured " +
                "with the seeded default client id before sign-up can attach new stakeholders to a client.");
        }

        services.Configure<ClientOnboardingOptions>(onboardingSection);

        return services.AddApplication();
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<StakeholderResolver>();
        services.AddScoped<GoogleSignUpHandler>();
        services.AddScoped<GoogleSignInHandler>();
        services.AddScoped<CompletePasswordResetHandler>();
        services.AddScoped<LogoutSessionHandler>();
        services.AddScoped<RefreshSessionHandler>();
        services.AddScoped<SignUpHandler>();
        services.AddScoped<SignUpOtpHandler>();
        services.AddScoped<SignInHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<ProcessMailtrapDeliveryWebhookHandler>();
        services.AddScoped<UploadAvatarHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<GetMyProfileHandler>();
        services.AddScoped<ActivateProviderHandler>();
        services.AddScoped<ActivatePaymentProviderHandler>();
        services.AddScoped<GetCountriesHandler>();
        services.AddScoped<GetCurrenciesHandler>();
        services.AddScoped<GetPaymentProvidersHandler>();
        services.AddScoped<InitiatePaymentHandler>();
        services.AddScoped<GetStakeholderWalletTransactionsHandler>();
        services.AddScoped<GetStakeholderWalletTopUpTransactionDetailHandler>();
        services.AddScoped<ProcessSafeHavenAccountCreditWebhookHandler>();
        services.AddScoped<ProcessSafeHavenAccountDebitWebhookHandler>();
        services.AddScoped<ProcessSafeHavenVirtualAccountTransferWebhookHandler>();
        services.AddScoped<ProcessCredoWebhookHandler>();
        services.AddScoped<PaymentReconciliationService>();
        services.AddScoped<CreatePropertyHandler>();
        services.AddScoped<ListPropertiesHandler>();
        services.AddScoped<AddUnitHandler>();
        services.AddScoped<ListUnitsHandler>();
        services.AddScoped<UpdateUnitRentHandler>();
        services.AddScoped<SetUnitAvailabilityHandler>();
        services.AddScoped<AllocateUnitHandler>();
        services.AddScoped<AcceptTenancyInvitationHandler>();
        services.AddScoped<ActivateTenancyHandler>();
        services.AddScoped<GetTenancyCycleHandler>();
        services.AddScoped<GetTenancySummaryHandler>();
        services.AddScoped<ListTenancyAllocationsHandler>();
        services.AddScoped<ListMyTenanciesHandler>();
        services.AddScoped<ListUpcomingRenewalsHandler>();
        services.AddScoped<RentReminderService>();
        services.AddScoped<RejectTenancyInvitationHandler>();
        services.AddScoped<UploadTenancyDocumentHandler>();
        services.AddScoped<RecordRentPaymentHandler>();
        services.AddScoped<ListTenancyRentPaymentsHandler>();
        services.AddScoped<TenancyDocumentAccessGuard>();
        services.AddScoped<ListTenancyDocumentsHandler>();
        services.AddScoped<GetTenancyDocumentDownloadUrlHandler>();

        return services;
    }
}

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
using TMG.Application.Tenancies.Features.AcceptTenancyInvitation;
using TMG.Application.Tenancies.Features.ActivateTenancy;
using TMG.Application.Tenancies.Features.AllocateUnit;
using TMG.Application.Tenancies.Features.GetTenancyCycle;
using TMG.Application.Tenancies.Features.ListUpcomingRenewals;
using TMG.Application.Tenancies.Features.ProcessRentReminders;
using TMG.Application.Tenancies.Features.RejectTenancyInvitation;
using TMG.Application.Tenancies.Features.UploadTenancyDocument;
using TMG.Application.Stakeholders.Features.UpdateProfile;
using TMG.Application.Stakeholders.Features.UploadAvatar;
using Microsoft.Extensions.DependencyInjection;

namespace TMG.Application;

public static class ServiceCollectionExtensions
{
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
        services.AddScoped<ActivateProviderHandler>();
        services.AddScoped<ActivatePaymentProviderHandler>();
        services.AddScoped<GetCountriesHandler>();
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
        services.AddScoped<ListUpcomingRenewalsHandler>();
        services.AddScoped<RentReminderService>();
        services.AddScoped<RejectTenancyInvitationHandler>();
        services.AddScoped<UploadTenancyDocumentHandler>();

        return services;
    }
}

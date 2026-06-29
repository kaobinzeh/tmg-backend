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
using TMG.Application.Providers.Features.ActivateProvider;
using TMG.Application.ReferenceData.Features.GetCountries;
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

        return services;
    }
}

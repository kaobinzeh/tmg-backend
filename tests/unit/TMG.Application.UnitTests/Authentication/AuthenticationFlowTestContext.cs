using TMG.Application.Authentication;
using TMG.Application.Authentication.Constants;
using TMG.Application.Authentication.Features.SignIn;
using TMG.Application.Authentication.Features.GoogleSignIn;
using TMG.Application.Authentication.Features.GoogleSignUp;
using TMG.Application.Authentication.Features.CompletePasswordReset;
using TMG.Application.Authentication.Features.LogoutSession;
using TMG.Application.Authentication.Features.RefreshSession;
using TMG.Application.Authentication.Features.RequestPasswordReset;
using TMG.Application.Authentication.Features.SignUp;
using TMG.Application.Authentication.Features.SignUpOtp;
using TMG.Application.Authentication.Stakeholders;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace TMG.Application.UnitTests.Authentication;

internal sealed class AuthenticationFlowTestContext
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
    public IAuthenticationIdentityService IdentityService { get; } = Substitute.For<IAuthenticationIdentityService>();
    public IGoogleIdentityTokenService GoogleIdentityTokenService { get; } = Substitute.For<IGoogleIdentityTokenService>();
    public IRefreshTokenService RefreshTokenService { get; } = Substitute.For<IRefreshTokenService>();
    public IAccessTokenRevocationService AccessTokenRevocationService { get; } = Substitute.For<IAccessTokenRevocationService>();
    public ITwoFactorOtpService TwoFactorOtpService { get; } = Substitute.For<ITwoFactorOtpService>();
    public IAccessTokenService AccessTokenService { get; } = Substitute.For<IAccessTokenService>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public IRepository<StakeholderType> StakeholderTypeRepository { get; } = Substitute.For<IRepository<StakeholderType>>();
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public StakeholderResolver StakeholderResolver => new(StakeholderRepository);
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IUnitOfWorkTransaction Transaction { get; } = Substitute.For<IUnitOfWorkTransaction>();
    public Guid DefaultClientId { get; } = Guid.CreateVersion7();

    public AuthenticationFlowTestContext()
    {
        UnitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Transaction));
    }

    public SignUpHandler CreateSignUpHandler() => new(
        IdentityService,
        EventPublisher,
        StakeholderTypeRepository,
        StakeholderRepository,
        CustomTelemetryContext,
        Options.Create(new ClientOnboardingOptions { DefaultClientId = DefaultClientId }),
        UnitOfWork);
    public GoogleSignUpHandler CreateGoogleSignUpHandler() => new(
        IdentityService,
        GoogleIdentityTokenService,
        EventPublisher,
        StakeholderTypeRepository,
        StakeholderRepository,
        CustomTelemetryContext,
        Options.Create(new ClientOnboardingOptions { DefaultClientId = DefaultClientId }),
        UnitOfWork);
    public SignUpOtpHandler CreateSignUpOtpHandler() => new(
        IdentityService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public SignInHandler CreateSignInHandler() => new(
        IdentityService,
        AccessTokenService,
        RefreshTokenService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public GoogleSignInHandler CreateGoogleSignInHandler() => new(
        IdentityService,
        GoogleIdentityTokenService,
        AccessTokenService,
        RefreshTokenService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public RefreshSessionHandler CreateRefreshSessionHandler() => new(
        IdentityService,
        AccessTokenService,
        RefreshTokenService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);
    public RequestPasswordResetHandler CreateRequestPasswordResetHandler() => new(
        IdentityService,
        CommandSender,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork);
    public CompletePasswordResetHandler CreateCompletePasswordResetHandler() => new(
        IdentityService,
        TwoFactorOtpService,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork);
    public LogoutSessionHandler CreateLogoutSessionHandler() => new(
        AccessTokenRevocationService,
        CustomTelemetryContext);

    private static ActorContext TestActorContext() => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7().ToString("N"),
        Guid.CreateVersion7().ToString("N"));

    public static SignUpCommand CreateSignUpCommand(
        string? email = null,
        string? password = null,
        Guid? countryId = null,
        string? firstName = null,
        string? lastName = null,
        string? stakeholderTypeKey = null)
    {
        var resolvedPassword = password ?? AuthenticationTestData.StrongPassword();

        return new SignUpCommand(
            email ?? AuthenticationTestData.Email(),
            resolvedPassword,
            resolvedPassword,
            countryId ?? Guid.CreateVersion7(),
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName(),
            stakeholderTypeKey ?? StakeholderDefaults.Types.TenantKey,
            TestActorContext());
    }

    public static SignInCommand CreateSignInCommand(
        string? email = null,
        string? password = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            email ?? AuthenticationTestData.Email(),
            password ?? AuthenticationTestData.StrongPassword(),
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public static GoogleSignUpCommand CreateGoogleSignUpCommand(
        string? idToken = null,
        Guid? countryId = null,
        string? firstName = null,
        string? lastName = null,
        string? stakeholderTypeKey = null) =>
        new(
            idToken ?? "google-id-token",
            countryId ?? Guid.CreateVersion7(),
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName(),
            stakeholderTypeKey ?? StakeholderDefaults.Types.TenantKey,
            TestActorContext());

    public static GoogleSignInCommand CreateGoogleSignInCommand(
        string? idToken = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            idToken ?? "google-id-token",
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public static SignUpOtpCommand CreateSignUpOtpCommand(
        string? email = null,
        string? otp = null) =>
        new(
            email ?? AuthenticationTestData.Email(),
            otp ?? AuthenticationTestData.Otp(),
            TestActorContext());

    public static RequestPasswordResetCommand CreateRequestPasswordResetCommand(string? email = null) =>
        new(email ?? AuthenticationTestData.Email(), TestActorContext());

    public static CompletePasswordResetCommand CreateCompletePasswordResetCommand(
        string? email = null,
        string? otp = null,
        string? password = null,
        string? confirmPassword = null)
    {
        var resolvedPassword = password ?? AuthenticationTestData.StrongPassword();

        return new CompletePasswordResetCommand(
            email ?? AuthenticationTestData.Email(),
            otp ?? AuthenticationTestData.Otp(),
            resolvedPassword,
            confirmPassword ?? resolvedPassword,
            TestActorContext());
    }

    public static RefreshSessionCommand CreateRefreshSessionCommand(
        string? refreshToken = null,
        string? ipAddress = null,
        string? userAgent = null) =>
        new(
            refreshToken ?? "refresh-token",
            ipAddress ?? AuthenticationTestData.IpAddress(),
            userAgent ?? AuthenticationTestData.UserAgent(),
            TestActorContext());

    public AppUser CreateUser(
        string? email = null,
        string? firstName = null,
        string? lastName = null) =>
        AppUser.Create(
            email ?? AuthenticationTestData.Email(),
            firstName ?? AuthenticationTestData.FirstName(),
            lastName ?? AuthenticationTestData.LastName());

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}



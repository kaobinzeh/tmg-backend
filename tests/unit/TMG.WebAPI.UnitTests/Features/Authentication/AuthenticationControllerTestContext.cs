using TMG.Application.Authentication;
using TMG.Application.Authentication.Constants;
using TMG.Application.Authentication.Features.CompletePasswordReset;
using TMG.Application.Authentication.Features.GoogleSignIn;
using TMG.Application.Authentication.Features.GoogleSignUp;
using TMG.Application.Authentication.Features.LogoutSession;
using TMG.Application.Authentication.Features.RefreshSession;
using TMG.Application.Authentication.Features.RequestPasswordReset;
using TMG.Application.Authentication.Features.ResendSignUpOtp;
using TMG.Application.Authentication.Features.SignIn;
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

namespace TMG.WebAPI.UnitTests.Features.Authentication;

internal sealed class AuthenticationControllerTestContext
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 21, 12, 0, 0, TimeSpan.Zero));
    public IAuthenticationIdentityService IdentityService { get; } = Substitute.For<IAuthenticationIdentityService>();
    public IGoogleIdentityTokenService GoogleIdentityTokenService { get; } = Substitute.For<IGoogleIdentityTokenService>();
    public IRefreshTokenService RefreshTokenService { get; } = Substitute.For<IRefreshTokenService>();
    public IAccessTokenRevocationService AccessTokenRevocationService { get; } = Substitute.For<IAccessTokenRevocationService>();
    public ITwoFactorOtpService TwoFactorOtpService { get; } = Substitute.For<ITwoFactorOtpService>();
    public IAccessTokenService AccessTokenService { get; } = Substitute.For<IAccessTokenService>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();
    public IRepository<StakeholderType> StakeholderTypeRepository { get; } = Substitute.For<IRepository<StakeholderType>>();
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IUnitOfWorkTransaction Transaction { get; } = Substitute.For<IUnitOfWorkTransaction>();
    public StakeholderResolver StakeholderResolver => new(StakeholderRepository);
    public Guid DefaultClientId { get; } = Guid.CreateVersion7();

    public AuthenticationControllerTestContext()
    {
        CurrentActor.ClientId.Returns(Guid.CreateVersion7());
        CurrentActor.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        CurrentActor.FlowId.Returns(Guid.CreateVersion7().ToString("N"));
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

    public LogoutSessionHandler CreateLogoutSessionHandler() => new(
        AccessTokenRevocationService,
        CustomTelemetryContext);

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

    public ResendSignUpOtpHandler CreateResendSignUpOtpHandler() => new(
        IdentityService,
        CommandSender,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork);

    public SignUpOtpHandler CreateSignUpOtpHandler() => new(
        IdentityService,
        TwoFactorOtpService,
        EventPublisher,
        StakeholderResolver,
        CustomTelemetryContext,
        UnitOfWork,
        Clock);

    public AppUser CreateUser(string? email = null, string? firstName = null, string? lastName = null) =>
        AppUser.Create(
            email ?? "jane@example.com",
            firstName ?? "Jane",
            lastName ?? "Doe");

    public Stakeholder CreateStakeholder(Guid appUserId) =>
        Stakeholder.Create(
            appUserId,
            CurrentActor.ClientId!.Value,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Jane",
            "Doe");

    public StakeholderType CreateStakeholderType() =>
        StakeholderType.Create(
            CurrentActor.ClientId!.Value,
            StakeholderDefaults.Types.TenantName,
            StakeholderDefaults.Types.TenantKey);

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}






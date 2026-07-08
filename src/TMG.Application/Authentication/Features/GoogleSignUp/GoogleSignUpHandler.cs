using TMG.Application.Authentication.Constants;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Stakeholders.Specifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TMG.Application.Authentication.Features.GoogleSignUp;

public sealed class GoogleSignUpHandler(
    IAuthenticationIdentityService identityService,
    IGoogleIdentityTokenService googleIdentityTokenService,
    IEventPublisher eventPublisher,
    IRepository<StakeholderType> stakeholderTypeRepository,
    IRepository<Stakeholder> stakeholderRepository,
    ICustomTelemetryContext customTelemetryContext,
    IOptions<ClientOnboardingOptions> clientOnboardingOptions,
    IUnitOfWork unitOfWork)
{
    public async Task<GoogleSignUpResult> HandleAsync(GoogleSignUpCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.GoogleSignUpStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        var googleIdentity = await googleIdentityTokenService.ValidateAsync(request.IdToken, cancellationToken);
        if (googleIdentity is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.InvalidGoogleToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.InvalidGoogleToken));
            return new GoogleSignUpResult(GoogleSignUpStatus.InvalidGoogleToken);
        }

        if (await identityService.FindByEmailAsync(googleIdentity.Email) is not null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateEmail);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateEmail));
            return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateEmail);
        }

        var user = AppUser.Create(googleIdentity.Email);
        user.MarkEmailVerified();

        var clientId = clientOnboardingOptions.Value.DefaultClientId
            ?? throw new InvalidOperationException(
                $"'{ClientOnboardingOptions.SectionName}:{nameof(ClientOnboardingOptions.DefaultClientId)}' must be configured to sign up.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var createResult = await identityService.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateEmail);
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Authentication.GoogleSignUpFailed,
                    ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateEmail));
                return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateEmail);
            }

            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            return new GoogleSignUpResult(GoogleSignUpStatus.ValidationFailed, ValidationErrors: createResult.ToValidationDictionary());
        }

        var addLoginResult = await identityService.AddLoginAsync(
            user,
            ExternalLoginProviders.Google,
            googleIdentity.Subject,
            ExternalLoginProviders.Google);
        if (!addLoginResult.Succeeded)
        {
            if (addLoginResult.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.LoginAlreadyAssociated)))
            {
                customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateGoogleAccount);
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Authentication.GoogleSignUpFailed,
                    ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateGoogleAccount));
                return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateGoogleAccount);
            }

            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            return new GoogleSignUpResult(GoogleSignUpStatus.ValidationFailed, ValidationErrors: addLoginResult.ToValidationDictionary());
        }

        var stakeholderType = await stakeholderTypeRepository.FirstOrDefaultAsync(
            new StakeholderTypeByClientAndKeySpecification(clientId, request.StakeholderTypeKey),
            cancellationToken);
        if (stakeholderType is null)
        {
            throw new InvalidOperationException(
                $"Stakeholder type '{request.StakeholderTypeKey}' is not configured for client '{clientId}'.");
        }

        var stakeholder = Stakeholder.Create(user.Id, clientId, request.CountryId, stakeholderType.Id, request.FirstName, request.LastName);
        await stakeholderRepository.AddAsync(stakeholder);

        await eventPublisher.PublishAsync(new UserCreated
        {
            StakeholderId = stakeholder.Id,
            ClientId = clientId,
            FlowId = request.ActorContext.FlowId
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.GoogleSignUpCompleted,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholder.Id));

        return new GoogleSignUpResult(GoogleSignUpStatus.Accepted, googleIdentity.Email);
    }
}







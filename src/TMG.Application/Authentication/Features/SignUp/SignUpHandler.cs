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

namespace TMG.Application.Authentication.Features.SignUp;

public sealed class SignUpHandler(
    IAuthenticationIdentityService identityService,
    IEventPublisher eventPublisher,
    IRepository<StakeholderType> stakeholderTypeRepository,
    IRepository<Stakeholder> stakeholderRepository,
    ICustomTelemetryContext customTelemetryContext,
    IOptions<ClientOnboardingOptions> clientOnboardingOptions,
    IUnitOfWork unitOfWork)
{
    public async Task<SignUpResult> HandleAsync(SignUpCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordSignUpStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        if (await identityService.FindByEmailAsync(request.Email) is not null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateEmail);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateEmail));
            return new SignUpResult(SignUpStatus.DuplicateEmail);
        }

        var user = AppUser.Create(request.Email);
        var clientId = clientOnboardingOptions.Value.DefaultClientId
            ?? throw new InvalidOperationException(
                $"'{ClientOnboardingOptions.SectionName}:{nameof(ClientOnboardingOptions.DefaultClientId)}' must be configured to sign up.");
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var createResult = await identityService.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateEmail);
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Authentication.PasswordSignUpFailed,
                    ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateEmail));
                return new SignUpResult(SignUpStatus.DuplicateEmail);
            }

            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            return new SignUpResult(SignUpStatus.ValidationFailed, createResult.ToValidationDictionary());
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
            Observability.EventNames.Authentication.PasswordSignUpCompleted,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholder.Id));

        return new SignUpResult(SignUpStatus.Accepted);
    }
}







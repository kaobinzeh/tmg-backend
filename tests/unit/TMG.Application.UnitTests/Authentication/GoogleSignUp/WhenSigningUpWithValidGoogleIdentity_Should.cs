using TMG.Application.Authentication.Features.GoogleSignUp;
using TMG.Application.UnitTests.Authentication;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;
using StakeholderDefaults = TMG.Application.Authentication.Constants.StakeholderDefaults;

namespace TMG.Application.UnitTests;

public sealed class WhenSigningUpWithValidGoogleIdentity_Should
{
    [Fact]
    public async Task ConfirmEmailAndPublishUserCreatedEvent()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var subject = Guid.CreateVersion7().ToString("N");
        var countryId = Guid.CreateVersion7();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var clientId = Guid.CreateVersion7();
        var stakeholderType = StakeholderType.Create(clientId, StakeholderDefaults.Types.TenantName, StakeholderDefaults.Types.TenantKey);

        context.GoogleIdentityTokenService.ValidateAsync("google-id-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleIdentityTokenPayload(subject, email, "Google User"));
        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>())
            .Returns(IdentityResult.Success);
        context.IdentityService.AddLoginAsync(
                Arg.Any<AppUser>(),
                Arg.Any<string>(),
                subject,
                Arg.Any<string>())
            .Returns(IdentityResult.Success);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);

        var result = await context.CreateGoogleSignUpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateGoogleSignUpCommand(
                idToken: "google-id-token",
                countryId: countryId,
                firstName: firstName,
                lastName: lastName),
            CancellationToken.None);

        result.Status.ShouldBe(GoogleSignUpStatus.Accepted);
        result.Email.ShouldBe(email);
        await context.IdentityService.Received(1).CreateAsync(
            Arg.Is<AppUser>(user =>
                user.Email == email &&
                user.UserName == email &&
                user.EmailConfirmed));
        await context.IdentityService.Received(1).AddLoginAsync(
            Arg.Any<AppUser>(),
            "Google",
            subject,
            "Google");
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserCreated>(message => message.StakeholderId != null),
            Arg.Any<CancellationToken>());
    }
}




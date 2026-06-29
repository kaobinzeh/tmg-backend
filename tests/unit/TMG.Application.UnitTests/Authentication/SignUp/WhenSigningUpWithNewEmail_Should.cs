using TMG.Application.Authentication.Features.SignUp;
using TMG.Application.UnitTests.Authentication;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;
using StakeholderDefaults = TMG.Application.Authentication.Constants.StakeholderDefaults;

namespace TMG.Application.UnitTests;

public sealed class WhenSigningUpWithNewEmail_Should
{
    [Fact]
    public async Task CreateIdentityUserAndPublishUserCreatedEvent()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var password = AuthenticationTestData.StrongPassword();
        var countryId = Guid.CreateVersion7();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var clientId = Guid.CreateVersion7();
        var stakeholderType = StakeholderType.Create(clientId, StakeholderDefaults.Types.TenantName, StakeholderDefaults.Types.TenantKey);

        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);

        var result = await context.CreateSignUpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpCommand(
                email: email,
                password: password,
                countryId: countryId,
                firstName: firstName,
                lastName: lastName),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.Accepted);
        await context.IdentityService.Received(1).CreateAsync(
            Arg.Is<AppUser>(user =>
                user.Email == email &&
                user.UserName == email &&
                user.EmailConfirmed == false),
            password);
        await context.StakeholderRepository.Received(1).AddAsync(
            Arg.Is<Domain.Stakeholders.Entities.Stakeholder>(stakeholder =>
                stakeholder.CountryId == countryId &&
                stakeholder.FirstName == firstName &&
                stakeholder.LastName == lastName),
            Arg.Any<CancellationToken>());
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserCreated>(message =>
                message.StakeholderId != null),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.Transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}




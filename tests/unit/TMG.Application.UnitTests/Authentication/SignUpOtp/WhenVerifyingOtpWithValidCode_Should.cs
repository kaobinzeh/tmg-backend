using TMG.Application.Authentication.Features.SignUpOtp;
using TMG.Application.UnitTests.Authentication;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests;

public sealed class WhenVerifyingOtpWithValidCode_Should
{
    [Fact]
    public async Task MarkUserAsVerified()
    {
        var email = AuthenticationTestData.Email();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var otp = AuthenticationTestData.Otp();

        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser(email, firstName, lastName);
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), firstName, lastName);

        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.VerifySignUpOtpAsync(user, otp).Returns(true);
        context.IdentityService.UpdateAsync(Arg.Is<AppUser>(candidate => candidate.EmailConfirmed)).Returns(IdentityResult.Success);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateSignUpOtpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpOtpCommand(email, otp),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpOtpStatus.Success);
        user.EmailConfirmed.ShouldBeTrue();
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserEmailConfirmed>(message => message.StakeholderId == stakeholder.Id),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.Transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}




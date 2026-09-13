using TMG.Application.Authentication.Features.ResendSignUpOtp;
using TMG.Application.UnitTests.Authentication;
using TMG.Contracts.Commands.Authentication;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests;

public sealed class WhenResendingSignUpOtp_Should
{
    [Fact]
    public async Task QueueASendEmailConfirmationOtpCommandForAnUnverifiedAccount()
    {
        var email = AuthenticationTestData.Email();
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser(email);
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe");

        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateResendSignUpOtpHandler().HandleAsync(
            new ResendSignUpOtpCommand(email, ActorContext.FromAnonymousActor(Substitute.For<ICurrentActor>())),
            CancellationToken.None);

        result.Status.ShouldBe(ResendSignUpOtpStatus.Accepted);
        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<SendEmailConfirmationOtpCommand>(command =>
                command.StakeholderId == stakeholder.Id && command.ClientId == stakeholder.ClientId),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotQueueAnythingForAnAlreadyVerifiedAccount()
    {
        var email = AuthenticationTestData.Email();
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser(email);
        user.MarkEmailVerified();

        context.IdentityService.FindByEmailAsync(email).Returns(user);

        var result = await context.CreateResendSignUpOtpHandler().HandleAsync(
            new ResendSignUpOtpCommand(email, ActorContext.FromAnonymousActor(Substitute.For<ICurrentActor>())),
            CancellationToken.None);

        result.Status.ShouldBe(ResendSignUpOtpStatus.AlreadyVerified);
        await context.CommandSender.DidNotReceive().SendAsync(
            Arg.Any<SendEmailConfirmationOtpCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotQueueAnythingForAnUnknownEmail()
    {
        var email = AuthenticationTestData.Email();
        var context = new AuthenticationFlowTestContext();

        context.IdentityService.FindByEmailAsync(email).Returns((TMG.Domain.Authentication.Entities.AppUser?)null);

        var result = await context.CreateResendSignUpOtpHandler().HandleAsync(
            new ResendSignUpOtpCommand(email, ActorContext.FromAnonymousActor(Substitute.For<ICurrentActor>())),
            CancellationToken.None);

        result.Status.ShouldBe(ResendSignUpOtpStatus.UserNotFound);
        await context.CommandSender.DidNotReceive().SendAsync(
            Arg.Any<SendEmailConfirmationOtpCommand>(),
            Arg.Any<CancellationToken>());
    }
}

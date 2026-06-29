using TMG.Application.Authentication.Features.RequestPasswordReset;
using TMG.Application.UnitTests.Authentication;
using TMG.Contracts.Commands.Authentication;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Stakeholders.Entities;
using Shouldly;

namespace TMG.Application.UnitTests;

public sealed class WhenRequestingPasswordResetWithKnownUserAndNoActiveOtp_Should
{
    [Fact]
    public async Task QueueResetPasswordCommand()
    {
        var context = new AuthenticationFlowTestContext();
        var clientId = Guid.CreateVersion7();
        var user = context.CreateUser();
        var stakeholder = Stakeholder.Create(
            user.Id,
            clientId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            AuthenticationTestData.FirstName(),
            AuthenticationTestData.LastName());

        context.IdentityService.FindByEmailAsync(user.Email!).Returns(user);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateRequestPasswordResetHandler().HandleAsync(
            new RequestPasswordResetCommand(user.Email!, new ActorContext(null, clientId, Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(RequestPasswordResetStatus.Success);
        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<ResetPasswordCommand>(command =>
                command.ClientId == clientId &&
                command.StakeholderId == stakeholder.Id),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}




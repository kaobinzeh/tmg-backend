using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Tenancies.ListAllocations;

public sealed class When_ListingAllocations_WithInvalidStatusFilter_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new TenanciesControllerTestContext();
        context.AuthenticateActor(Guid.CreateVersion7(), Guid.CreateVersion7());

        var sut = context.CreateController();

        var result = await sut.ListAllocations("not-a-status", CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}

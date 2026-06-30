using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests;

public sealed class WhenRejectingTenancy_Should
{
    [Fact]
    public void MarkRejected()
    {
        var tenancy = Tenancy.Invite(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "tenant@example.com");
        var rejectedAt = new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero);

        tenancy.Reject(rejectedAt);

        tenancy.Status.ShouldBe(TenancyStatus.Rejected);
        tenancy.RejectedAtUtc.ShouldBe(rejectedAt);
    }
}

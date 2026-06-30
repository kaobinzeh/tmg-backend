using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests;

public sealed class WhenAcceptingTenancy_Should
{
    [Fact]
    public void MarkAcceptedAndRecordTerms()
    {
        var tenancy = Tenancy.Invite(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "tenant@example.com");
        var acceptedAt = new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero);

        tenancy.Accept(acceptedAt);

        tenancy.Status.ShouldBe(TenancyStatus.Accepted);
        tenancy.AcceptedAtUtc.ShouldBe(acceptedAt);
        tenancy.TermsAcceptedAtUtc.ShouldBe(acceptedAt);
    }
}

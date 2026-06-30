using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests;

public sealed class WhenInvitingTenancy_Should
{
    [Fact]
    public void StartInvitedWithNormalizedEmail()
    {
        var clientId = Guid.CreateVersion7();
        var propertyId = Guid.CreateVersion7();
        var unitId = Guid.CreateVersion7();
        var tenantStakeholderId = Guid.CreateVersion7();

        var tenancy = Tenancy.Invite(clientId, propertyId, unitId, tenantStakeholderId, "  Tenant@Example.COM  ");

        tenancy.ClientId.ShouldBe(clientId);
        tenancy.PropertyId.ShouldBe(propertyId);
        tenancy.UnitId.ShouldBe(unitId);
        tenancy.TenantStakeholderId.ShouldBe(tenantStakeholderId);
        tenancy.InvitedEmail.ShouldBe("tenant@example.com");
        tenancy.Status.ShouldBe(TenancyStatus.Invited);
        tenancy.AcceptedAtUtc.ShouldBeNull();
    }
}

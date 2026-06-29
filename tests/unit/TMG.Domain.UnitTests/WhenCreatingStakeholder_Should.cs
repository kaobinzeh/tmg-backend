using TMG.Domain.Stakeholders.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests;

public sealed class WhenCreatingStakeholder_Should
{
    [Fact]
    public void SetStakeholderTypeAndAuditFields()
    {
        var appUserId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderTypeId = Guid.CreateVersion7();
        const string firstName = "Ada";
        const string lastName = "Lovelace";
        var now = new DateTimeOffset(2026, 4, 6, 0, 0, 0, TimeSpan.Zero);

        var stakeholder = Stakeholder.Create(appUserId, clientId, countryId, stakeholderTypeId, firstName, lastName);

        stakeholder.AppUserId.ShouldBe(appUserId);
        stakeholder.ClientId.ShouldBe(clientId);
        stakeholder.CountryId.ShouldBe(countryId);
        stakeholder.StakeholderTypeId.ShouldBe(stakeholderTypeId);
        stakeholder.FirstName.ShouldBe(firstName);
        stakeholder.LastName.ShouldBe(lastName);
        stakeholder.IsVerified.ShouldBeFalse();
        stakeholder.CreatedAtUtc.ShouldBe(default);
        stakeholder.UpdatedAtUtc.ShouldBe(default);
    }
}



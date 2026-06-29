using TMG.Domain.Properties.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests;

public sealed class WhenCreatingProperty_Should
{
    [Fact]
    public void SetDetailsAndOwnership()
    {
        var clientId = Guid.CreateVersion7();
        var ownerStakeholderId = Guid.CreateVersion7();

        var property = Property.Create(clientId, ownerStakeholderId, "  Lekki Court  ", "  12 Admiralty Way  ", "  Block of flats  ");

        property.ClientId.ShouldBe(clientId);
        property.OwnerStakeholderId.ShouldBe(ownerStakeholderId);
        property.Name.ShouldBe("Lekki Court");
        property.Address.ShouldBe("12 Admiralty Way");
        property.Description.ShouldBe("Block of flats");
    }
}

using TMG.Domain.Stakeholders.Entities;

namespace TMG.Domain.UnitTests;

public sealed class WhenCreatingClient_Should
{
    [Fact]
    public void SetFieldsAndAuditDates()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var clientId = Guid.CreateVersion7();

        var client = Client.Create(clientId, " Moveaex ", " Moveaex ");

        client.Id.ShouldBe(clientId);
        client.Name.ShouldBe("Moveaex");
        client.BrandKey.ShouldBe("moveaex");
        client.CreatedAtUtc.ShouldBe(default);
        client.UpdatedAtUtc.ShouldBe(default);
    }
}



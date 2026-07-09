using TMG.WebAPI.Features.Properties;
using TMG.WebAPI.IntegrationTests.Tenancies;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Properties.CreateProperty;

[Collection(nameof(ContainersCollection))]
public sealed class When_CreatingProperty_AsTenant_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task ReturnForbidden()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: true, acceptedTenancy: false);
        await AuthenticateAsTenantAsync(tenant.Email);

        await WhenCreatingAProperty();
        ThenTheRequestIsForbidden();

        async Task WhenCreatingAProperty()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Properties.V1,
                new CreatePropertyRequest("Tenant Towers", "1 Escalation Close", null));
        }

        void ThenTheRequestIsForbidden()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

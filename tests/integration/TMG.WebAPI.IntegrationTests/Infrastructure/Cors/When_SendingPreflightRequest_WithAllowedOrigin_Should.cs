using TMG.WebAPI.IntegrationTests.Infrastructure;
using Shouldly;
using System.Net;

namespace TMG.WebAPI.IntegrationTests.Infrastructure.Cors;

[Collection(nameof(ContainersCollection))]
public sealed class When_SendingPreflightRequest_WithAllowedOrigin_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(
        fixture,
        new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = AllowedOrigin
        }), IAsyncLifetime
{
    private const string AllowedOrigin = "http://localhost:3000";

    private HttpResponseMessage? _response;

    public async Task InitializeAsync() => await InitializeClientAsync();

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task AllowTheConfiguredOrigin()
    {
        await WhenSendingPreflightRequest();
        ThenTheOriginIsAllowed();

        async Task WhenSendingPreflightRequest()
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, EndpointUrl.Countries.V1);
            request.Headers.Add("Origin", AllowedOrigin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "authorization,x-client-id");

            _response = await Client.SendAsync(request);
        }

        void ThenTheOriginIsAllowed()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            _response.Headers.GetValues("Access-Control-Allow-Origin").ShouldContain(AllowedOrigin);
            string.Join(",", _response.Headers.GetValues("Access-Control-Allow-Headers"))
                .ShouldContain("authorization", Case.Insensitive);
        }
    }
}

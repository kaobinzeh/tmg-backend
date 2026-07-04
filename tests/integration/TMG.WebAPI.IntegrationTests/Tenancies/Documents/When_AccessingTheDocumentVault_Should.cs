using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using TMG.Application.Tenancies.Features.ListTenancyDocuments;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Providers.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Infrastructure.Storage;
using TMG.WebAPI.Features.Tenancies;
using TMG.WebAPI.IntegrationTests.Tenancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace TMG.WebAPI.IntegrationTests.Tenancies.Documents;

[Collection(nameof(ContainersCollection))]
public sealed class When_AccessingTheDocumentVault_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task LetTheTenantListTheirDocumentsAndGetASignedDownloadUrl()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        await SeedFileStorageProviderAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: true, acceptedTenancy: true);
        await AuthenticateAsTenantAsync(tenant.Email);

        var documentId = await UploadDocumentAsync();

        await ThenListingReturnsTheDocument();
        await ThenADownloadUrlIsIssued();

        async Task<Guid> UploadDocumentAsync()
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(TenancyDocumentType.NationalId.ToString()), "DocumentType");
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-id-bytes"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "File", "national-id.png");

            var uploadResponse = await Client.PostAsync(EndpointUrl.Tenancies.DocumentsV1, content);
            uploadResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
            var payload = await uploadResponse.Content.ReadFromJsonAsync<UploadTenancyDocumentResponse>();
            payload.ShouldNotBeNull();
            uploadResponse.Dispose();
            return payload.DocumentId;
        }

        async Task ThenListingReturnsTheDocument()
        {
            _response = await Client.GetAsync(EndpointUrl.Tenancies.DocumentsForTenancyV1(tenant.TenancyId));
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var documents = await _response.Content.ReadFromJsonAsync<List<TenancyDocumentListItem>>();
            documents.ShouldNotBeNull();
            documents.ShouldContain(document => document.Id == documentId && document.DocumentType == TenancyDocumentType.NationalId);
        }

        async Task ThenADownloadUrlIsIssued()
        {
            using var downloadResponse = await Client.GetAsync(EndpointUrl.Tenancies.DocumentDownloadUrlV1(documentId));
            downloadResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var payload = await downloadResponse.Content.ReadFromJsonAsync<DocumentDownloadUrlResponse>();
            payload.ShouldNotBeNull();
            payload.Url.ShouldNotBeNullOrWhiteSpace();
            payload.ExpiresAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        }
    }

    private async Task SeedFileStorageProviderAsync()
    {
        using var scope = CreateScope();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await providerRepository.AddAsync(Provider.Create(ProviderType.FileStorage, "Noop", ObjectStorageProviderKeys.Noop, true));
        await unitOfWork.SaveChangesAsync();
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();
            var providers = await dbContext.Providers
                .Where(provider => provider.ProviderType == ProviderType.FileStorage && provider.ProviderKey == ObjectStorageProviderKeys.Noop)
                .ToListAsync();
            dbContext.Providers.RemoveRange(providers);
            await dbContext.SaveChangesAsync();
        }

        await base.DisposeAsync();
    }
}

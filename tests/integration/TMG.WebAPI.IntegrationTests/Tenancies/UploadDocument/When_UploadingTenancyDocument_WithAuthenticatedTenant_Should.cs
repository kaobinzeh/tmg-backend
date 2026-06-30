using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Providers.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.WebAPI.Features.Tenancies;
using TMG.WebAPI.IntegrationTests.Tenancies;
using TMG.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace TMG.WebAPI.IntegrationTests.Tenancies.UploadDocument;

[Collection(nameof(ContainersCollection))]
public sealed class When_UploadingTenancyDocument_WithAuthenticatedTenant_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task PersistTheDocument()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        await SeedFileStorageProviderAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: true, acceptedTenancy: true);
        await AuthenticateAsTenantAsync(tenant.Email);

        UploadTenancyDocumentResponse? payload = null;

        await WhenUploadingDocument();
        await ThenTheDocumentIsPersisted();

        async Task WhenUploadingDocument()
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(TenancyDocumentType.NationalId.ToString()), "DocumentType");
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-id-bytes"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "File", "national-id.png");

            _response = await Client.PostAsync(EndpointUrl.Tenancies.DocumentsV1, content);
        }

        async Task ThenTheDocumentIsPersisted()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Created);

            payload = await _response.Content.ReadFromJsonAsync<UploadTenancyDocumentResponse>();
            payload.ShouldNotBeNull();

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();
            var document = await dbContext.TenancyDocuments.FirstOrDefaultAsync(item => item.Id == payload.DocumentId);

            document.ShouldNotBeNull();
            document.TenancyId.ShouldBe(tenant.TenancyId);
            document.DocumentType.ShouldBe(TenancyDocumentType.NationalId);
            document.StorageKey.ShouldNotBeNullOrWhiteSpace();
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

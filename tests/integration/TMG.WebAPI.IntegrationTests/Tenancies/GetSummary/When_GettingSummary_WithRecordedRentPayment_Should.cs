using TMG.Domain.Common.Persistence;
using TMG.Domain.Providers.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.WebAPI.Features.Tenancies;
using TMG.WebAPI.IntegrationTests.Tenancies;
using TMG.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Tenancies.GetSummary;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingSummary_WithRecordedRentPayment_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private sealed record SummaryView(decimal CollectedAmount, decimal OutstandingAmount, int UpcomingRenewalsCount);

    [Fact]
    public async Task ReturnCollectedAmountAndUpcomingRenewals()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        await SeedFileStorageProviderAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: true, acceptedTenancy: true);

        // A short term so the renewal date falls inside the default six-month summary horizon.
        (await Client.PostAsJsonAsync(
            EndpointUrl.Tenancies.ActivateV1(tenant.TenancyId),
            new ActivateTenancyRequest(DateTimeOffset.UtcNow, TermMonths: 3))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await Client.PostAsJsonAsync(
            EndpointUrl.Tenancies.PaymentsV1(tenant.TenancyId),
            new RecordRentPaymentRequest(1_500_000m, RentPaymentMethod.BankTransfer, "TRF-SUMMARY"))).StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await Client.GetAsync(EndpointUrl.Tenancies.SummaryV1);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<SummaryView>();
        summary.ShouldNotBeNull();
        summary.CollectedAmount.ShouldBe(1_500_000m);
        summary.OutstandingAmount.ShouldBe(0m);
        summary.UpcomingRenewalsCount.ShouldBeGreaterThanOrEqualTo(1);
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

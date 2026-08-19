using System.Net;
using System.Net.Http.Json;
using TMG.Application.Tenancies.Features.ListTenancyRentPayments;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Providers.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.WebAPI.Features.Tenancies;
using TMG.WebAPI.IntegrationTests.Tenancies;
using TMG.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace TMG.WebAPI.IntegrationTests.Tenancies.RecordRentPayment;

[Collection(nameof(ContainersCollection))]
public sealed class When_RecordingRentPayment_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task RecordTheInitialAndRenewalPaymentsArchivingAReceiptForEach()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        await SeedFileStorageProviderAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: true, acceptedTenancy: true);
        var leaseStart = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

        // The manager activates the rent cycle, then records payments. (Client is manager-authenticated.)
        (await Client.PostAsJsonAsync(
            EndpointUrl.Tenancies.ActivateV1(tenant.TenancyId),
            new ActivateTenancyRequest(leaseStart, TermMonths: 12))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await ThenTheInitialPaymentSettlesTheCurrentTerm();
        await ThenTheRenewalPaymentAdvancesTheCycle();
        await ThenBothPaymentsCanBeListedForTheTenancy();

        async Task ThenTheInitialPaymentSettlesTheCurrentTerm()
        {
            var payload = await RecordPaymentAsync("TRF-INITIAL");

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();

            var payment = await dbContext.RentPayments.FirstOrDefaultAsync(item => item.Id == payload.RentPaymentId);
            payment.ShouldNotBeNull();
            payment.TenancyId.ShouldBe(tenant.TenancyId);
            payment.Amount.ShouldBe(1_500_000m);
            payment.PeriodStartUtc.ShouldBe(leaseStart);
            payment.PeriodEndUtc.ShouldBe(leaseStart.AddMonths(12));
            payment.ReceiptDocumentId.ShouldBe(payload.ReceiptDocumentId);

            var receipt = await dbContext.TenancyDocuments.FirstOrDefaultAsync(item => item.Id == payload.ReceiptDocumentId);
            receipt.ShouldNotBeNull();
            receipt.DocumentType.ShouldBe(TenancyDocumentType.Receipt);

            // First payment settles the current term without advancing the cycle.
            var tenancy = await dbContext.Tenancies.FirstAsync(item => item.Id == tenant.TenancyId);
            tenancy.CycleEndUtc.ShouldBe(leaseStart.AddMonths(12));
            tenancy.NextRentDueUtc.ShouldBe(leaseStart.AddMonths(12));
        }

        async Task ThenTheRenewalPaymentAdvancesTheCycle()
        {
            var payload = await RecordPaymentAsync("TRF-RENEWAL");

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();

            var payment = await dbContext.RentPayments.FirstOrDefaultAsync(item => item.Id == payload.RentPaymentId);
            payment.ShouldNotBeNull();
            payment.PeriodStartUtc.ShouldBe(leaseStart.AddMonths(12));
            payment.PeriodEndUtc.ShouldBe(leaseStart.AddMonths(24));

            // Renewal payment rolls the cycle forward by one term.
            var tenancy = await dbContext.Tenancies.FirstAsync(item => item.Id == tenant.TenancyId);
            tenancy.CycleEndUtc.ShouldBe(leaseStart.AddMonths(24));
            tenancy.NextRentDueUtc.ShouldBe(leaseStart.AddMonths(24));
        }

        async Task ThenBothPaymentsCanBeListedForTheTenancy()
        {
            _response?.Dispose();
            _response = await Client.GetAsync(EndpointUrl.Tenancies.PaymentsV1(tenant.TenancyId));
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var payments = await _response.Content.ReadFromJsonAsync<IReadOnlyList<RentPaymentListItem>>();
            payments.ShouldNotBeNull();
            payments.Count.ShouldBe(2);
            payments.Select(payment => payment.Reference).ShouldBe(["TRF-INITIAL", "TRF-RENEWAL"], ignoreOrder: true);
            payments.ShouldAllBe(payment => payment.Amount == 1_500_000m);
            payments.ShouldAllBe(payment => payment.Method == nameof(RentPaymentMethod.BankTransfer));
            payments.ShouldAllBe(payment => payment.ReceiptDocumentId != null);
            payments.ShouldAllBe(payment => payment.ReceiptNumber.StartsWith("RCPT-"));
        }

        async Task<RecordRentPaymentResponse> RecordPaymentAsync(string reference)
        {
            _response?.Dispose();
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Tenancies.PaymentsV1(tenant.TenancyId),
                new RecordRentPaymentRequest(1_500_000m, RentPaymentMethod.BankTransfer, reference));

            _response.StatusCode.ShouldBe(HttpStatusCode.Created);
            var payload = await _response.Content.ReadFromJsonAsync<RecordRentPaymentResponse>();
            payload.ShouldNotBeNull();
            payload.ReceiptDocumentId.ShouldNotBeNull();
            return payload;
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

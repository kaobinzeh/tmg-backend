using TMG.Contracts.Events;
using TMG.Domain.Authentication.Persistence;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Persistence;
using TMG.Domain.ReferenceData.Entities;
using TMG.Domain.Stakeholders.Entities;
using TMG.WebAPI.Features.Authentication.Registrations;
using TMG.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenSigningUpWithGoogleIdentity_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture, DefaultClientConfiguration), IAsyncLifetime
{
    private static readonly Guid DefaultClientId = Guid.CreateVersion7();
    private static readonly Dictionary<string, string?> DefaultClientConfiguration = new()
    {
        ["Clients:Onboarding:DefaultClientId"] = DefaultClientId.ToString()
    };

    private string _email = string.Empty;
    private Guid _clientId;
    private Guid _countryId;
    private bool _createdCountryForTest;
    private string _idToken = string.Empty;
    private GoogleSignUpRequest _request = default!;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _clientId = DefaultClientId;
        _countryId = await ResolveCountryIdAsync();
        await EnsureDefaultStakeholderTypeExistsAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteAuthenticationRecordsAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnAcceptedAndConfirmEmail()
    {
        await GivenAValidGoogleIdentity();
        await WhenSigningUpWithGoogle();
        await ThenTheRequestIsAcceptedAndTheEmailIsConfirmed();

        async Task GivenAValidGoogleIdentity()
        {
            _email = WebApiIntegrationTestData.Email();
            _idToken = $"google-sign-up-{Guid.CreateVersion7():N}";
            GoogleIdentityTokenService.Register(
                _idToken,
                new GoogleIdentityTokenPayload(Guid.CreateVersion7().ToString("N"), _email, "Google User"));

            _request = new GoogleSignUpRequest(
                _idToken,
                _countryId,
                WebApiIntegrationTestData.FirstName(),
                WebApiIntegrationTestData.LastName(),
                "tenant");

            await Task.CompletedTask;
        }

        async Task WhenSigningUpWithGoogle()
        {
            _response = await Client.PostAsJsonAsync(EndpointUrl.GoogleRegistrations.V1, _request);
        }

        async Task ThenTheRequestIsAcceptedAndTheEmailIsConfirmed()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

            using var scope = CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
            var outboxRepository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
            var user = await userRepository.GetByEmailAsync(_email);
            var outboxMessages = await outboxRepository.ListAsync(new UserCreatedOutboxMessagesSpecification());

            user.ShouldNotBeNull();
            user.EmailConfirmed.ShouldBeTrue();
            outboxMessages.Count.ShouldBe(1);
        }
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            return;
        }

        using var scope = CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await userRepository.GetByEmailAsync(_email);

        if (user is not null)
        {
            var stakeholders = await stakeholderRepository.ListAsync(new StakeholderByAppUserIdCleanupSpecification(user.Id));
            foreach (var stakeholder in stakeholders)
            {
                stakeholderRepository.Remove(stakeholder);
            }
        }

        if (user is not null)
        {
            userRepository.Remove(user);
        }

        var outboxMessages = await outboxRepository.ListAsync(new UserCreatedOutboxMessagesSpecification());
        foreach (var outboxMessage in outboxMessages)
        {
            outboxRepository.Remove(outboxMessage);
        }

        var stakeholderTypes = await stakeholderTypeRepository.ListAsync(new DefaultCustomerStakeholderTypeCleanupSpecification(_clientId));
        foreach (var stakeholderType in stakeholderTypes)
        {
            stakeholderTypeRepository.Remove(stakeholderType);
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(_countryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
            }
        }

        if (user is not null || outboxMessages.Count > 0 || stakeholderTypes.Count > 0 || _createdCountryForTest)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }

    private async Task<Guid> ResolveCountryIdAsync()
    {
        using var scope = CreateScope();
        var countryReadRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var countryWriteRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var countries = await countryReadRepository.ListAsync(new FirstCountrySpecification());
        if (countries.Count > 0)
        {
            return countries[0].Id;
        }

        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg");
        await countryWriteRepository.AddAsync(country);
        await unitOfWork.SaveChangesAsync();
        _createdCountryForTest = true;

        return country.Id;
    }

    private async Task EnsureDefaultStakeholderTypeExistsAsync()
    {
        using var scope = CreateScope();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var stakeholderType = StakeholderType.Create(_clientId, "Tenant", "tenant");

        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await unitOfWork.SaveChangesAsync();
    }

    private sealed class UserCreatedOutboxMessagesSpecification : Specification<OutboxMessage>
    {
        public UserCreatedOutboxMessagesSpecification()
        {
            Where(message =>
                message.Kind == OutboxMessageKind.Event &&
                message.Type == typeof(UserCreated).FullName!);
        }
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification()
        {
            ApplyPaging(0, 1);
        }
    }

    private sealed class DefaultCustomerStakeholderTypeCleanupSpecification : Specification<StakeholderType>
    {
        public DefaultCustomerStakeholderTypeCleanupSpecification(Guid clientId)
        {
            Where(stakeholderType => stakeholderType.ClientId == clientId && stakeholderType.Key == "tenant");
        }
    }

    private sealed class StakeholderByAppUserIdCleanupSpecification : Specification<Stakeholder>
    {
        public StakeholderByAppUserIdCleanupSpecification(Guid appUserId)
        {
            Where(stakeholder => stakeholder.AppUserId == appUserId);
        }
    }
}






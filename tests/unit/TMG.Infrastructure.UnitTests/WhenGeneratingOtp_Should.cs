using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Caching;
using TMG.Infrastructure.Authentication;
using Shouldly;

namespace TMG.Infrastructure.UnitTests;

public sealed class WhenGeneratingOtp_Should
{
    [Fact]
    public async Task StoreAnOtpThatExists()
    {
        var cache = new InMemoryJsonCache();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 17, 0, 0, 0, TimeSpan.Zero));
        var service = new TwoFactorOtpService(cache, clock);
        var userId = Guid.CreateVersion7();

        var otp = await service.GenerateOtpAsync(
            userId,
            OtpIntent.PasswordReset,
            CancellationToken.None,
            6,
            false);

        otp.Code.Length.ShouldBe(6);
        otp.Code.All(char.IsDigit).ShouldBeTrue();
        (await service.OtpExistsAsync(userId, OtpIntent.PasswordReset, CancellationToken.None)).ShouldBeTrue();
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemoryJsonCache : IJsonCache
    {
        private readonly Dictionary<string, object> _store = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            _store.TryGetValue(key, out var value);
            return Task.FromResult((T?)value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _store[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }
    }
}


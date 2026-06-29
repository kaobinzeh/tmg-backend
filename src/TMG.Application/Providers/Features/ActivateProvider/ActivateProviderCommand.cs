using TMG.Domain.Providers.Entities;

namespace TMG.Application.Providers.Features.ActivateProvider;

public sealed record ActivateProviderCommand(ProviderType ProviderType, string ProviderKey);

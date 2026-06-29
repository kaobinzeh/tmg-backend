using Asp.Versioning;
using TMG.Application.Providers.Features.ActivateProvider;
using TMG.Domain.Common.Authentication;
using TMG.WebAPI.Infrastructure;
using TMG.WebAPI.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Providers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Providers.Route)]
public sealed class ProvidersController(
    ActivateProviderHandler activateProviderHandler,
    ActivateProviderValidator validator) : ControllerBase
{
    [HttpPut("active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateProvider(
        [FromBody] ActivateProviderRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        _ = Enum.TryParse(request.ProviderType, true, out Domain.Providers.Entities.ProviderType providerType);

        var result = await activateProviderHandler.HandleAsync(
            new ActivateProviderCommand(providerType, request.ProviderKey),
            cancellationToken);

        return result.Status switch
        {
            ActivateProviderStatus.ProviderNotFound => NotFound(result.Error ?? "Provider not found."),
            _ => NoContent()
        };
    }
}

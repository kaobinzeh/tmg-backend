using Asp.Versioning;
using TMG.Application.ReferenceData.Features.GetCurrencies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.ReferenceData.Currencies;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.Currencies.Route)]
public sealed class CurrenciesController(GetCurrenciesHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GetCurrenciesResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GetCurrenciesResponse>>> Handle(CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(cancellationToken);
        return Ok(response);
    }
}

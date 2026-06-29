using System.Diagnostics;
using System.Security.Claims;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Infrastructure;

public sealed class CurrentActorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentActorAccessor currentActorAccessor,
        IStakeholderReadModelRepository stakeholderReadModelRepository,
        IProblemDetailsService problemDetailsService)
    {
        Guid? clientId = null;
        if (Guid.TryParse(context.Request.Headers["X-Client-Id"], out var parsedClientId))
        {
            clientId = parsedClientId;
        }

        if (!IsExcludedFromClientCheck(context.Request.Path))
        {
            if (!clientId.HasValue)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Missing client identifier",
                        Detail = "The X-Client-Id header is required."
                    }
                });
                return;
            }
        }

        var actorId = await ResolveActorIdAsync(context, stakeholderReadModelRepository);

        var correlationId = context.TraceIdentifier;
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Activity.Current?.Id ?? Guid.CreateVersion7().ToString("N");
        }

        var flowId = ResolveFlowId(context);
        currentActorAccessor.Set(actorId, clientId, correlationId, flowId);
        context.Response.Headers[Domain.Common.Observability.Observability.FlowIdHeaderName] = flowId;
        await next(context);
    }

    private static bool IsExcludedFromClientCheck(PathString path)
    {
        return path.StartsWithSegments("/health")
            || path.StartsWithSegments("/metrics")
            || path.StartsWithSegments("/openapi")
            || path.StartsWithSegments("/scalar")
            || path == "/"
            || path.StartsWithSegments("/api/v1/payments/webhooks")
            || path.StartsWithSegments("/api/v1/email-notifications/webhooks");
}

    private static async Task<string> ResolveActorIdAsync(
        HttpContext context,
        IStakeholderReadModelRepository stakeholderReadModelRepository)
    {
        var stakeholderId = context.User.FindFirstValue(CustomClaimTypes.StakeholderId);
        if (Guid.TryParse(stakeholderId, out var parsedStakeholderId))
        {
            return parsedStakeholderId.ToString();
        }

        var appUserId =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        if (Guid.TryParse(appUserId, out var parsedAppUserId))
        {
            var stakeholder = await stakeholderReadModelRepository.GetByAppUserIdAsync(parsedAppUserId, context.RequestAborted);
            if (stakeholder is not null)
            {
                return stakeholder.StakeholderId.ToString();
            }
        }

        return (context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : null)
            ?? "anonymous";
    }

    private static string ResolveFlowId(HttpContext context)
    {
        var flowId = context.Request.Headers[Domain.Common.Observability.Observability.FlowIdHeaderName].ToString();
        return string.IsNullOrWhiteSpace(flowId)
            ? Guid.CreateVersion7().ToString("N")
            : flowId;
    }
}

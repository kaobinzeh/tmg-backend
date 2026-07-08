using System.Diagnostics;
using System.Security.Claims;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Stakeholders.ReadModels;

namespace TMG.WebAPI.Infrastructure;

public sealed class CurrentActorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentActorAccessor currentActorAccessor,
        IStakeholderReadModelRepository stakeholderReadModelRepository)
    {
        var (actorId, clientId) = await ResolveActorAsync(context, stakeholderReadModelRepository);

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

    private static async Task<(string ActorId, Guid? ClientId)> ResolveActorAsync(
        HttpContext context,
        IStakeholderReadModelRepository stakeholderReadModelRepository)
    {
        Guid? clientId = null;
        if (Guid.TryParse(context.User.FindFirstValue(CustomClaimTypes.ClientId), out var parsedClientId))
        {
            clientId = parsedClientId;
        }

        string? actorId = null;
        if (Guid.TryParse(context.User.FindFirstValue(CustomClaimTypes.StakeholderId), out var parsedStakeholderId))
        {
            actorId = parsedStakeholderId.ToString();
        }

        if (actorId is null || clientId is null)
        {
            var appUserId =
                context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");
            if (Guid.TryParse(appUserId, out var parsedAppUserId))
            {
                var stakeholder = await stakeholderReadModelRepository.GetByAppUserIdAsync(parsedAppUserId, context.RequestAborted);
                if (stakeholder is not null)
                {
                    actorId ??= stakeholder.StakeholderId.ToString();
                    clientId ??= stakeholder.ClientId;
                }
            }
        }

        actorId ??=
            (context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : null)
            ?? "anonymous";

        return (actorId, clientId);
    }

    private static string ResolveFlowId(HttpContext context)
    {
        var flowId = context.Request.Headers[Domain.Common.Observability.Observability.FlowIdHeaderName].ToString();
        return string.IsNullOrWhiteSpace(flowId)
            ? Guid.CreateVersion7().ToString("N")
            : flowId;
    }
}

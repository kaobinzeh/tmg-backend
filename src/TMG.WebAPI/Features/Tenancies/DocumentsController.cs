using Asp.Versioning;
using TMG.Application.Tenancies.Features.GetTenancyDocumentDownloadUrl;
using TMG.Application.Tenancies.Features.ListTenancyDocuments;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMG.WebAPI.Features.Tenancies;

/// <summary>
/// Per-unit document vault reads: a manager sees every tenancy's documents for their client; a tenant sees only
/// their own tenancy's. Downloads are served as time-limited signed URLs.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.Tenancies.Route)]
public sealed class DocumentsController(
    ListTenancyDocumentsHandler listTenancyDocumentsHandler,
    GetTenancyDocumentDownloadUrlHandler getTenancyDocumentDownloadUrlHandler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet("allocations/{tenancyId:guid}/documents")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<IReadOnlyList<TenancyDocumentListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TenancyDocumentListItem>>> ListDocuments(
        Guid tenancyId,
        CancellationToken cancellationToken)
    {
        var result = await listTenancyDocumentsHandler.HandleAsync(
            new ListTenancyDocumentsCommand(tenancyId, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            ListTenancyDocumentsStatus.NotAuthenticated => Unauthorized(),
            ListTenancyDocumentsStatus.TenancyNotFound => NotFound(),
            ListTenancyDocumentsStatus.Forbidden => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "You do not have access to this tenancy's documents."),
            _ => Ok(result.Documents)
        };
    }

    [HttpGet("documents/{documentId:guid}/download-url")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<DocumentDownloadUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDownloadUrlResponse>> GetDownloadUrl(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await getTenancyDocumentDownloadUrlHandler.HandleAsync(
            new GetTenancyDocumentDownloadUrlCommand(documentId, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            GetTenancyDocumentDownloadUrlStatus.NotAuthenticated => Unauthorized(),
            GetTenancyDocumentDownloadUrlStatus.DocumentNotFound => NotFound(),
            GetTenancyDocumentDownloadUrlStatus.Forbidden => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "You do not have access to this document."),
            _ => Ok(new DocumentDownloadUrlResponse(result.Url!, result.ExpiresAtUtc!.Value))
        };
    }
}

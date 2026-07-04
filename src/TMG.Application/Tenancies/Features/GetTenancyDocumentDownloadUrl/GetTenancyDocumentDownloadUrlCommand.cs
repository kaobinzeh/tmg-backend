using TMG.Domain.Common.Auditing;

namespace TMG.Application.Tenancies.Features.GetTenancyDocumentDownloadUrl;

public sealed record GetTenancyDocumentDownloadUrlCommand(Guid DocumentId, ActorContext ActorContext);

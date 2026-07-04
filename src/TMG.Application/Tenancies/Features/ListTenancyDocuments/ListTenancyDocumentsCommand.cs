using TMG.Domain.Common.Auditing;

namespace TMG.Application.Tenancies.Features.ListTenancyDocuments;

public sealed record ListTenancyDocumentsCommand(Guid TenancyId, ActorContext ActorContext);

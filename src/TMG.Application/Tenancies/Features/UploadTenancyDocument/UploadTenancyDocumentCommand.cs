using TMG.Domain.Common.Auditing;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Application.Tenancies.Features.UploadTenancyDocument;

public sealed record UploadTenancyDocumentCommand(
    TenancyDocumentType DocumentType,
    Stream Content,
    string FileName,
    string ContentType,
    long ContentLength,
    ActorContext ActorContext);

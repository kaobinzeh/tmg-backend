namespace TMG.Application.Common.Pagination;

public sealed record CursorPaginationPayload(DateTimeOffset CreatedAtUtc, Guid EntityId);

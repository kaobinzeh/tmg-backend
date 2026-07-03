using TMG.Domain.Tenancies.Entities;

namespace TMG.Application.Tenancies.Features.GetTenancyCycle;

public sealed record GetTenancyCycleResult(
    GetTenancyCycleStatus Status,
    TenancyCycleDto? Cycle = null);

public sealed record TenancyCycleDto(
    Guid TenancyId,
    Guid UnitId,
    TenancyStatus Status,
    DateTimeOffset? CycleStartUtc,
    DateTimeOffset? CycleEndUtc,
    DateTimeOffset? NextRentDueUtc,
    DateTimeOffset? Reminder3MonthsSentAtUtc,
    DateTimeOffset? Reminder1MonthSentAtUtc);

public enum GetTenancyCycleStatus
{
    Success = 1,
    NotAuthenticated = 2,
    TenancyNotFound = 3
}

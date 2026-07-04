namespace TMG.Domain.Tenancies.Entities;

/// <summary>The lease term a rent payment covers: the window from the previous cycle end to the new one.</summary>
public readonly record struct RentPeriod(DateTimeOffset StartUtc, DateTimeOffset EndUtc);

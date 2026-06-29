namespace TMG.WebAPI.Features.Properties;

public sealed record CreatePropertyRequest(
    string Name,
    string Address,
    string? Description);

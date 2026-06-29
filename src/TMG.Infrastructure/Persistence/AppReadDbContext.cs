using Microsoft.EntityFrameworkCore;

namespace TMG.Infrastructure.Persistence;

public sealed class AppReadDbContext(DbContextOptions<AppReadDbContext> options)
    : AppDbContextBase<AppReadDbContext>(options);

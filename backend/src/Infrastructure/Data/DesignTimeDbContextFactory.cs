using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

/// <summary>
/// Lets the EF Core tools (<c>dotnet ef migrations add</c>, …) build the model without starting the API, so they
/// don't need its settings. Commands that reach a database take one explicitly:
/// <c>dotnet ef database update --connection "…"</c>.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options);
}

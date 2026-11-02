using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TestUtilities.Database;
using Xunit;

namespace TestUtilities.Base;

/// <summary>Gives each test its own PostgreSQL database, copied from the migrated template, and drops it afterwards.</summary>
public abstract class IntegrationTestBase(PostgresFixture database) : IAsyncLifetime
{
    private string _connectionString = string.Empty;

    protected AppDbContext Context { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _connectionString = await database.CreateDatabaseAsync();
        Context = CreateContext();
    }

    /// <summary>
    /// A new context on this test's database. Concurrency tests need one per simultaneous operation:
    /// a DbContext can't run two operations at once.
    /// </summary>
    protected AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);

    protected async Task SeedAsync<T>(params T[] entities)
    {
        foreach (var entity in entities)
        {
            if (entity == null) continue;
            await Context.AddAsync(entity);
        }
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    protected void ClearTracking()
    {
        Context.ChangeTracker.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            NpgsqlConnection.ClearPool(connection);
        }
        await database.DropDatabaseAsync(_connectionString);
    }
}

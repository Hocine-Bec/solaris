using Application.Common.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TestUtilities.Database;
using Xunit;

namespace TestUtilities.Base;

/// <summary>Gives each test its own PostgreSQL database, copied from the migrated template, and drops it afterwards.</summary>
public abstract class IntegrationTestBase(PostgresFixture database) : IAsyncLifetime
{
    /// <summary>This test's database.</summary>
    protected string ConnectionString { get; private set; } = string.Empty;

    protected AppDbContext Context { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        ConnectionString = await database.CreateDatabaseAsync();
        Context = CreateContext();
    }

    /// <summary>
    /// A new context on this test's database. Concurrency tests need one per simultaneous operation:
    /// a DbContext can't run two operations at once.
    /// </summary>
    protected AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

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

    /// <summary>Saves this test's changes the way services do: once, through the unit of work.</summary>
    protected Task<SaveOutcome> SaveAsync() =>
        new UnitOfWork(Context).SaveAsync(TestContext.Current.CancellationToken);

    protected void ClearTracking()
    {
        Context.ChangeTracker.Clear();
    }

    public virtual async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            NpgsqlConnection.ClearPool(connection);
        }
        await database.DropDatabaseAsync(ConnectionString);
    }
}

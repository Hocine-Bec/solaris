using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace TestUtilities.Database;

/// <summary>
/// One PostgreSQL container for the whole test run. The migrations are applied once to a template
/// database, and every test gets its own copy of it, so tests never share data and can run in parallel.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string TemplateDatabase = "solaris_template";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();

    // Copying fails if another session is using the template at the same moment, so copies are made one at a time.
    private readonly SemaphoreSlim _copyLock = new(1, 1);

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await ExecuteAsync($"CREATE DATABASE {TemplateDatabase}");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionStringFor(TemplateDatabase))
            .Options;
        await using (var context = new AppDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        // The template can only be copied once no connection to it is left open.
        NpgsqlConnection.ClearAllPools();
    }

    /// <summary>Creates a new database from the migrated template and returns its connection string.</summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"test_{Guid.NewGuid():N}";
        await _copyLock.WaitAsync();
        try
        {
            await ExecuteAsync($"CREATE DATABASE {name} TEMPLATE {TemplateDatabase}");
        }
        finally
        {
            _copyLock.Release();
        }

        return ConnectionStringFor(name);
    }

    public async Task DropDatabaseAsync(string connectionString)
    {
        var name = new NpgsqlConnectionStringBuilder(connectionString).Database;
        await ExecuteAsync($"DROP DATABASE IF EXISTS {name} WITH (FORCE)");
    }

    public async ValueTask DisposeAsync()
    {
        _copyLock.Dispose();
        await _container.DisposeAsync();
    }

    private string ConnectionStringFor(string database) =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = database }.ConnectionString;

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

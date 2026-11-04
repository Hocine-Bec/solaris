using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TestUtilities.Base;
using TestUtilities.Database;

namespace IntegrationTests.Infrastructure;

// Every name in the migrated schema is snake_case, so hand-written SQL never needs quoted identifiers.
public partial class DatabaseNamingTests(PostgresFixture database) : IntegrationTestBase(database)
{
    // EF Core's own table of applied migrations keeps its PascalCase names.
    private const string MigrationsHistoryTable = "__EFMigrationsHistory";

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex SnakeCase();

    [Fact]
    public async Task MigratedSchema_EveryTableColumnConstraintAndIndexName_IsSnakeCase()
    {
        // Arrange - every name in the public schema, read from the database the migrations built
        const string sql = """
            SELECT 'table', table_name, table_name
            FROM information_schema.tables WHERE table_schema = 'public'
            UNION ALL
            SELECT 'column', table_name, column_name
            FROM information_schema.columns WHERE table_schema = 'public'
            UNION ALL
            SELECT 'constraint', c.relname, con.conname
            FROM pg_constraint con JOIN pg_class c ON c.oid = con.conrelid
            WHERE con.connamespace = 'public'::regnamespace
            UNION ALL
            SELECT 'index', tablename, indexname
            FROM pg_indexes WHERE schemaname = 'public'
            """;

        // Act
        var names = new List<(string Kind, string Table, string Name)>();
        await using (var connection = new NpgsqlConnection(Context.Database.GetConnectionString()))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
                names.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        // Assert
        names.Should().Contain(n => n.Kind == "table" && n.Name == "leads", "the migrations must have run");
        var offenders = names
            .Where(n => n.Table != MigrationsHistoryTable && !SnakeCase().IsMatch(n.Name))
            .Select(n => $"{n.Kind} {n.Table}.{n.Name}")
            .ToList();
        offenders.Should().BeEmpty("every name must be snake_case, but these aren't: {0}", string.Join(", ", offenders));
    }
}

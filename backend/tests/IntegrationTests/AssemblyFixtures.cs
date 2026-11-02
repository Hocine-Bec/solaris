using TestUtilities.Database;

// One PostgreSQL container shared by every test class in this assembly.
[assembly: AssemblyFixture(typeof(PostgresFixture))]

using FluentAssertions;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestUtilities.Base;
using TestUtilities.Database;

namespace IntegrationTests.Infrastructure;

// Hangfire's storage belongs to the host that built it, not to the process: several hosts in one process (the
// integration tests) each keep to their own database.
public class HangfireStorageTests(PostgresFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public void JobStorage_IsTheHostsOwn_WhateverTheProcessWideOneIs()
    {
        // Arrange - this host's services, on this test's database
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ConnectionString,
            ["Jwt:Secret"] = new string('s', 32),
            ["Email:FromAddress"] = "sales@example.com",
            ["Email:SiteUrl"] = "https://solaris.test"
        }).Build();
        var services = new ServiceCollection().AddLogging()
            .AddSingleton<IHostEnvironment>(new TestHostEnvironment("Testing"))
            .AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var storage = provider.GetRequiredService<JobStorage>();
        var manager = provider.GetRequiredService<IRecurringJobManager>();

        // Assert - one registration, the host's own storage, which the job manager uses too
        services.Count(d => d.ServiceType == typeof(JobStorage)).Should().Be(1);
        storage.Should().BeSameAs(provider.GetRequiredService<PostgreSqlStorage>());
        manager.Should().NotBeNull();
    }
}

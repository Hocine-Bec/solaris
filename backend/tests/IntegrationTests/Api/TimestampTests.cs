using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// Every incoming timestamp must carry Z or an offset, and is stored as UTC. No endpoint takes a timestamp today (the
// server sets a lead's times since the lead screens), so the rule is checked on the API's own request-body settings,
// which every future endpoint gets.
public class TimestampTests(PostgresFixture database) : ApiTestBase(database)
{
    private sealed record Stamped(DateTime At);

    private JsonSerializerOptions RequestBodyOptions()
    {
        CreateClient();
        return ApiServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
    }

    [Theory]
    [InlineData("2026-11-02T10:00:00Z")]
    [InlineData("2026-11-02T11:00:00+01:00")]
    [InlineData("2026-11-02T04:00:00-06:00")]
    public void Timestamp_WithAZone_IsReadAsTheSameInstantInUtc(string at)
    {
        // Regression: an offset arrived as local time, and Npgsql refused it with a 500.
        var read = JsonSerializer.Deserialize<Stamped>($"{{\"at\":\"{at}\"}}", RequestBodyOptions())!;

        read.At.Should().Be(new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc));
        read.At.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Timestamp_WithoutAZone_IsRefused()
    {
        // Regression: a timestamp without a zone answered 500.
        var read = () => JsonSerializer.Deserialize<Stamped>("{\"at\":\"2026-11-02T10:00:00\"}", RequestBodyOptions());

        read.Should().Throw<JsonException>().WithMessage("*must include a time zone*");
    }
}

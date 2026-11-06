using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Json;

/// <summary>
/// The one rule for every timestamp in a request body: it must say which time zone it is in, with <c>Z</c> or an
/// offset (<c>2026-11-02T10:00:00Z</c>, <c>2026-11-02T10:00:00+01:00</c>), and it is converted to UTC. A timestamp
/// without one is rejected, since "10:00" alone could be UTC or the client's local time. Responses are written as
/// they are: every stored timestamp is already UTC.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTime(out var parsed))
            throw new JsonException("The value must be an ISO 8601 timestamp, such as 2026-11-02T10:00:00Z.");

        // The reader marks a timestamp without Z or an offset as Unspecified.
        if (parsed.Kind == DateTimeKind.Unspecified)
            throw new JsonException(
                "The timestamp must include a time zone: Z for UTC, or an offset such as +01:00.");

        // Read again with its offset kept, so the conversion to UTC is exact.
        reader.TryGetDateTimeOffset(out var withOffset);
        return withOffset.UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}

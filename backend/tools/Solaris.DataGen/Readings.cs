using Domain.Common;
using Npgsql;
using NpgsqlTypes;

namespace Solaris.DataGen;

/// <summary>
/// The readings themselves, written with <c>COPY</c>: 11 million rows one at a time would take hours, and the
/// dataset isn't what P4 measures. Each installation's days are its own local days (BR-P4), so a day is 92, 96 or
/// 100 slots where the clocks change.
/// </summary>
public static class Readings
{
    /// <summary>Rows written between progress lines.</summary>
    private const int ReportEvery = 1_000_000;

    /// <summary>The columns each layout writes.</summary>
    private static string[] ColumnsOf(Schema schema) => schema == Schema.Naive
        ? ["installation_id", "recorded_at", "energy_kwh"]
        : ["installation_id", "slot_start", "energy_wh"];

    /// <summary>
    /// Why this layout can't be written: the table hasn't the columns it needs. The final layout arrives with its
    /// own migration, so asking for it against a database that predates it says so rather than failing inside COPY.
    /// </summary>
    public static async Task<string?> WhyCantWriteAsync(NpgsqlConnection connection, Schema schema)
    {
        var wanted = ColumnsOf(schema);
        await using var command = new NpgsqlCommand(
            "SELECT column_name FROM information_schema.columns WHERE table_name = 'energy_readings'", connection);
        var found = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                found.Add(reader.GetString(0));

        if (found.Count == 0)
            return "There is no energy_readings table: apply the migrations first.";
        var missing = wanted.Where(column => !found.Contains(column, StringComparer.Ordinal)).ToList();
        return missing.Count == 0
            ? null
            : $"The {schema.ToString().ToLowerInvariant()} layout needs {string.Join(", ", missing)} in " +
              $"energy_readings, which has {string.Join(", ", found)}. Apply the migration that makes it.";
    }

    public static async Task<long> WriteAsync(
        NpgsqlConnection connection, IReadOnlyList<GeneratedInstallation> fleet, Options options, Random random,
        DateOnly lastDay, Action<long> onProgress)
    {
        await using var writer = await connection.BeginBinaryImportAsync(
            $"COPY energy_readings ({string.Join(", ", ColumnsOf(options.Schema))}) FROM STDIN (FORMAT BINARY)");

        long written = 0, sinceReport = 0;
        foreach (var installation in fleet)
        {
            var (low, high) = ProductionShape.Reaches(installation.Condition);
            for (var back = options.Days - 1; back >= 0; back--)
            {
                var day = lastDay.AddDays(-back);
                // One day in a hundred the system says nothing at all: a dead link, a power cut
                if (random.NextDouble() < 0.01)
                    continue;

                var (fromUtc, toUtc) = LocalDays.WindowUtc(day, installation.TimeZone);
                var slots = (int)((toUtc - fromUtc) / EnergyReadingSlot);
                var reaches = low + (decimal)random.NextDouble() * (high - low);
                var cloud = 0.3m + (decimal)random.NextDouble() * 0.7m;
                var dayKwh = Production.ExpectedKwh(installation.SystemSizeKw, installation.State, day.Month) * reaches * cloud;
                var weights = ProductionShape.SlotWeights(slots, ProductionShape.DaylightHours(day));
                var mostPerSlot = Domain.Entities.EnergyReading.MaximumKwh(installation.SystemSizeKw);

                for (var slot = 0; slot < slots; slot++)
                {
                    if (weights[slot] <= 0)
                        continue;
                    var kwh = Math.Min(Math.Round(dayKwh * (decimal)weights[slot], 4), mostPerSlot);
                    if (kwh <= 0)
                        continue;

                    await writer.StartRowAsync();
                    await writer.WriteAsync(installation.Id, NpgsqlDbType.Integer);
                    await writer.WriteAsync(fromUtc.AddMinutes(15 * slot), NpgsqlDbType.TimestampTz);
                    if (options.Schema == Schema.Naive)
                        await writer.WriteAsync(kwh, NpgsqlDbType.Numeric);
                    else
                        await writer.WriteAsync((int)Math.Round(kwh * 1000m), NpgsqlDbType.Integer);

                    written++;
                    if (++sinceReport < ReportEvery)
                        continue;
                    sinceReport = 0;
                    onProgress(written);
                }
            }
        }

        await writer.CompleteAsync();
        return written;
    }

    private static readonly TimeSpan EnergyReadingSlot = Domain.Entities.EnergyReading.Slot;
}

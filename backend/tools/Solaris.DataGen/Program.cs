using System.Diagnostics;
using System.Globalization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Solaris.DataGen;

// Fills a throwaway database with a fleet and its readings, for the P4 measurements (measurements.md §5). It writes
// nothing but what it is pointed at, and refuses a database that already holds installations or readings.

var (options, problem) = Options.Parse(args);
if (options is null)
{
    await Console.Error.WriteLineAsync(problem);
    await Console.Error.WriteLineAsync();
    await Console.Error.WriteLineAsync(Options.Usage);
    return 2;
}

var started = Stopwatch.StartNew();
var random = new Random(options.Seed);

var builder = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(options.ConnectionString);
await using var context = new AppDbContext(builder.Options);

if (await Fleet.WhyNotEmptyAsync(context) is { } notEmpty)
{
    await Console.Error.WriteLineAsync(notEmpty);
    return 1;
}

// The readings end with yesterday, the last day that has certainly finished everywhere
var lastDay = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
var firstDay = lastDay.AddDays(-(options.Days - 1));
var activatedAt = new DateTime(firstDay.AddDays(-1), TimeOnly.MinValue, DateTimeKind.Utc);

Console.WriteLine($"{options.Installations:N0} installations, " +
                  (options.Days == 0 ? "no readings (the fleet only), " : $"{options.Days} days to {lastDay:yyyy-MM-dd}, ") +
                  $"seed {options.Seed}, {options.Schema.ToString().ToLowerInvariant()} layout");

// Before anything is written: a layout the table can't hold would otherwise leave a fleet behind with no readings
await using var connection = new NpgsqlConnection(options.ConnectionString);
await connection.OpenAsync();
if (await Readings.WhyCantWriteAsync(connection, options.Schema) is { } cantWrite)
{
    await Console.Error.WriteLineAsync(cantWrite);
    return 1;
}

var fleet = await Fleet.CreateAsync(context, options, random, activatedAt);
await Fleet.WriteKeysAsync(fleet, options.KeysPath);
Console.WriteLine($"  {fleet.Count:N0} installations, their keys in {options.KeysPath} ({started.Elapsed.TotalSeconds:0.0} s)");

// --days 0 is the fleet alone: the ingestion run (P4a) uploads the readings itself
var readings = options.Days == 0
    ? 0
    : await Readings.WriteAsync(connection, fleet, options, random, lastDay,
        written => Console.WriteLine($"  {written:N0} readings ({started.Elapsed.TotalSeconds:0.0} s)"));

Console.WriteLine($"  {readings:N0} readings written in {started.Elapsed.TotalSeconds:0.0} s " +
                  $"({readings / Math.Max(started.Elapsed.TotalSeconds, 0.001):N0} rows/s)");

// What the dataset turned out to be, so a measurement's record can quote it rather than the intention
var byCondition = fleet.GroupBy(i => i.Condition).OrderBy(g => g.Key)
    .Select(g => $"{g.Count():N0} {g.Key.ToString().ToLowerInvariant()}");
Console.WriteLine($"  systems: {string.Join(", ", byCondition)}; " +
                  $"{fleet.Select(i => i.TimeZone).Distinct().Count()} time zones, " +
                  $"{fleet.Select(i => i.State).Distinct().Count()} states, " +
                  $"{fleet.Min(i => i.SystemSizeKw).ToString("0.#", CultureInfo.InvariantCulture)}–" +
                  $"{fleet.Max(i => i.SystemSizeKw).ToString("0.#", CultureInfo.InvariantCulture)} kW");
return 0;

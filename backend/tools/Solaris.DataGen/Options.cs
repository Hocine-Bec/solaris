using System.Globalization;

namespace Solaris.DataGen;

/// <summary>Which table layout the readings are written in: the before P4 measures, or the after.</summary>
public enum Schema
{
    /// <summary>`energy_readings` as EF Core's defaults make it: a surrogate key, `recorded_at`, `energy_kwh`.</summary>
    Naive,

    /// <summary>The fixed layout (4.5): keyed by the installation and the slot, `slot_start`, `energy_wh`.</summary>
    Final
}

/// <summary>What the generator was asked for.</summary>
public sealed record Options(
    int Installations,
    int Days,
    int Seed,
    Schema Schema,
    string ConnectionString,
    string KeysPath)
{
    public const string Usage = """
        Fills a database with Active installations and their readings, for the P4 measurements.

          dotnet run --project backend/tools/Solaris.DataGen -- [options]

          --installations <n>   how many Active installations to create (default 1000)
          --days <n>            how many days of readings each one gets, ending yesterday (default 120);
                                0 makes the fleet and its keys only, for the ingestion run to fill
          --seed <n>            the same seed always makes the same dataset (default 42)
          --schema naive|final  which layout to write the readings in (default naive)
          --connection <text>   the database; by default ConnectionStrings__Default from the environment
          --keys <path>         where the device keys are written (default perf/keys.csv)

        It only ever writes to a database you point it at, and it refuses one that already holds readings.
        """;

    /// <summary>Reads the arguments, or says what is wrong with them.</summary>
    public static (Options? Options, string? Problem) Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i += 2)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length)
                return (null, $"'{args[i]}' isn't an option with a value.");
            values[args[i][2..]] = args[i + 1];
        }
        foreach (var name in values.Keys)
            if (name is not ("installations" or "days" or "seed" or "schema" or "connection" or "keys"))
                return (null, $"'--{name}' isn't an option.");

        if (!Count(values, "installations", 1000, out var installations, out var problem) ||
            !Count(values, "days", 120, out var days, out problem, least: 0) ||
            !Count(values, "seed", 42, out var seed, out problem))
            return (null, problem);

        var schemaName = values.GetValueOrDefault("schema", "naive");
        if (!Enum.TryParse<Schema>(schemaName, ignoreCase: true, out var schema))
            return (null, $"'--schema {schemaName}' isn't a layout: it is naive or final.");

        var connection = values.GetValueOrDefault("connection")
                         ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        if (string.IsNullOrWhiteSpace(connection))
            return (null, "No database: pass --connection, or set ConnectionStrings__Default.");

        return (new Options(installations, days, seed, schema, connection, values.GetValueOrDefault("keys", "perf/keys.csv")), null);
    }

    private static bool Count(Dictionary<string, string> values, string name, int fallback, out int count,
        out string? problem, int least = 1)
    {
        problem = null;
        count = fallback;
        if (!values.TryGetValue(name, out var text))
            return true;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out count) || count < least)
        {
            problem = least == 0
                ? $"'--{name} {text}' isn't a whole number, or is below zero."
                : $"'--{name} {text}' isn't a whole number above zero.";
            return false;
        }
        return true;
    }
}

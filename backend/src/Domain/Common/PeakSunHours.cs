namespace Domain.Common;

/// <summary>
/// The peak sun hours a state gets each month (BR-P1): the average energy a square metre of flat ground receives in a
/// day, in kWh/m², which is the same number as the hours of full sun it stands for. Expected production for a day is
/// the system's size in kW × this month's hours × 0.80.
///
/// One representative city stands for each state: the most populous, and Washington for the District of Columbia. The
/// numbers come from the lab's published solar data, read on 2026-11-23 from its Solar Resource API
/// (<c>avg_ghi</c>, the Perez-SUNY/NREL 2012 dataset). That dataset stops at the lower 48 and Hawaii, so Alaska's row
/// comes from PVWatts at the same coordinates with the array flat, which measures the same thing. Each city, its
/// coordinates and the source of its row are in <c>docs/data/peak-sun-hours.json</c>, which this table is generated
/// from.
///
/// Weather isn't in them: they are long-term averages, so a cloudy week makes a system look worse than it is. The
/// fleet comparison, not this table, is what judges a system's health.
/// </summary>
public static class PeakSunHours
{
    /// <summary>The 12 monthly averages per state, January first.</summary>
    private static readonly IReadOnlyDictionary<string, decimal[]> ByState = new Dictionary<string, decimal[]>(StringComparer.Ordinal)
    {
        ["AL"] = [2.35m, 3.03m, 4.23m, 5.26m, 5.92m, 6.25m, 5.94m, 5.71m, 4.74m, 3.74m, 2.74m, 2.15m], // Huntsville
        ["AK"] = [0.29m, 0.92m, 2.75m, 4.24m, 5.32m, 5.55m, 4.86m, 4.02m, 2.52m, 1.12m, 0.41m, 0.17m], // Anchorage
        ["AZ"] = [3.46m, 4.31m, 5.76m, 7.16m, 7.96m, 8.31m, 7.49m, 6.81m, 6.12m, 4.94m, 3.80m, 3.18m], // Phoenix
        ["AR"] = [2.37m, 3.12m, 4.10m, 5.38m, 5.75m, 6.35m, 6.32m, 5.99m, 4.88m, 3.67m, 2.72m, 2.24m], // Little Rock
        ["CA"] = [3.06m, 3.65m, 5.15m, 6.35m, 6.89m, 7.24m, 7.65m, 7.02m, 5.79m, 4.42m, 3.46m, 2.82m], // Los Angeles
        ["CO"] = [2.51m, 3.52m, 4.74m, 5.72m, 6.59m, 7.21m, 7.11m, 6.19m, 5.35m, 3.89m, 2.78m, 2.26m], // Denver
        ["CT"] = [1.83m, 2.76m, 3.81m, 4.75m, 5.51m, 5.81m, 5.96m, 5.29m, 4.28m, 2.95m, 2.02m, 1.66m], // Bridgeport
        ["DE"] = [2.08m, 2.97m, 4.03m, 4.97m, 5.71m, 6.01m, 6.07m, 5.37m, 4.40m, 3.26m, 2.28m, 1.89m], // Wilmington
        ["DC"] = [2.20m, 3.15m, 4.11m, 4.99m, 5.69m, 6.09m, 6.00m, 5.40m, 4.41m, 3.35m, 2.38m, 1.98m], // Washington
        ["FL"] = [3.20m, 3.94m, 5.08m, 6.14m, 6.58m, 6.00m, 5.94m, 5.50m, 4.56m, 4.05m, 3.38m, 2.84m], // Jacksonville
        ["GA"] = [2.71m, 3.41m, 4.49m, 5.48m, 6.18m, 6.33m, 6.01m, 5.57m, 4.60m, 3.84m, 2.97m, 2.34m], // Atlanta
        ["HI"] = [4.16m, 4.82m, 5.61m, 6.37m, 6.78m, 7.10m, 6.95m, 6.84m, 6.25m, 5.14m, 4.22m, 3.82m], // Honolulu
        ["ID"] = [1.60m, 2.68m, 3.97m, 5.54m, 6.84m, 7.64m, 7.89m, 6.89m, 5.38m, 3.56m, 1.88m, 1.35m], // Boise
        ["IL"] = [1.78m, 2.56m, 3.63m, 4.79m, 5.51m, 6.16m, 6.18m, 5.27m, 4.44m, 2.90m, 1.89m, 1.45m], // Chicago
        ["IN"] = [1.91m, 2.64m, 3.78m, 4.81m, 5.64m, 6.19m, 6.07m, 5.46m, 4.62m, 3.16m, 2.08m, 1.62m], // Indianapolis
        ["IA"] = [1.77m, 2.63m, 3.72m, 4.94m, 5.74m, 6.17m, 6.36m, 5.52m, 4.62m, 3.05m, 2.06m, 1.64m], // Des Moines
        ["KS"] = [2.48m, 3.33m, 4.22m, 5.43m, 6.18m, 6.65m, 6.86m, 6.14m, 4.96m, 3.62m, 2.72m, 2.21m], // Wichita
        ["KY"] = [1.96m, 2.88m, 3.84m, 4.92m, 5.83m, 6.28m, 5.99m, 5.68m, 4.64m, 3.39m, 2.28m, 1.73m], // Louisville
        ["LA"] = [2.93m, 3.70m, 4.79m, 5.88m, 6.33m, 5.94m, 5.55m, 5.34m, 4.76m, 4.21m, 3.42m, 2.74m], // New Orleans
        ["ME"] = [1.82m, 2.65m, 3.70m, 4.85m, 5.44m, 5.65m, 5.93m, 5.36m, 4.33m, 2.84m, 1.80m, 1.55m], // Portland
        ["MD"] = [2.15m, 3.03m, 4.11m, 4.99m, 5.69m, 6.13m, 6.04m, 5.37m, 4.34m, 3.32m, 2.38m, 1.97m], // Baltimore
        ["MA"] = [1.87m, 2.79m, 3.76m, 4.73m, 5.33m, 5.61m, 5.86m, 5.30m, 4.29m, 2.92m, 1.93m, 1.60m], // Boston
        ["MI"] = [1.62m, 2.54m, 3.74m, 4.88m, 5.53m, 6.09m, 6.10m, 5.22m, 4.40m, 2.76m, 1.72m, 1.37m], // Detroit
        ["MN"] = [1.58m, 2.44m, 3.59m, 4.91m, 5.49m, 5.95m, 6.35m, 5.31m, 4.17m, 2.57m, 1.70m, 1.36m], // Minneapolis
        ["MS"] = [2.60m, 3.37m, 4.45m, 5.67m, 6.14m, 6.42m, 6.14m, 5.83m, 4.82m, 4.01m, 3.07m, 2.41m], // Jackson
        ["MO"] = [2.20m, 2.97m, 4.00m, 4.96m, 5.92m, 6.32m, 6.36m, 5.68m, 4.75m, 3.34m, 2.39m, 2.01m], // Kansas City
        ["MT"] = [1.47m, 2.49m, 3.59m, 5.00m, 6.06m, 6.60m, 7.26m, 6.12m, 4.57m, 2.87m, 1.76m, 1.27m], // Billings
        ["NE"] = [1.96m, 2.80m, 3.85m, 5.02m, 5.86m, 6.34m, 6.44m, 5.60m, 4.75m, 3.23m, 2.24m, 1.75m], // Omaha
        ["NV"] = [3.17m, 3.98m, 5.64m, 6.93m, 7.94m, 8.37m, 7.65m, 7.03m, 6.22m, 4.71m, 3.49m, 2.87m], // Las Vegas
        ["NH"] = [1.84m, 2.66m, 3.78m, 4.82m, 5.33m, 5.53m, 5.87m, 5.25m, 4.20m, 2.84m, 1.85m, 1.57m], // Manchester
        ["NJ"] = [2.03m, 2.96m, 3.98m, 4.82m, 5.54m, 5.84m, 5.87m, 5.15m, 4.29m, 3.08m, 2.12m, 1.77m], // Newark
        ["NM"] = [3.35m, 4.20m, 5.51m, 6.88m, 7.66m, 7.97m, 7.36m, 6.70m, 5.92m, 4.71m, 3.63m, 3.00m], // Albuquerque
        ["NY"] = [2.00m, 2.89m, 3.91m, 4.77m, 5.51m, 5.85m, 5.85m, 5.17m, 4.30m, 3.03m, 2.08m, 1.75m], // New York
        ["NC"] = [2.65m, 3.43m, 4.49m, 5.53m, 6.22m, 6.40m, 6.06m, 5.64m, 4.73m, 3.72m, 2.89m, 2.38m], // Charlotte
        ["ND"] = [1.06m, 1.90m, 3.21m, 5.20m, 5.68m, 6.09m, 6.51m, 5.47m, 4.07m, 2.43m, 1.55m, 0.97m], // Fargo
        ["OH"] = [1.82m, 2.51m, 3.61m, 4.80m, 5.58m, 6.08m, 5.91m, 5.36m, 4.51m, 3.10m, 2.01m, 1.57m], // Columbus
        ["OK"] = [2.71m, 3.48m, 4.33m, 5.60m, 6.03m, 6.58m, 6.98m, 6.30m, 5.14m, 3.83m, 2.89m, 2.49m], // Oklahoma City
        ["OR"] = [1.16m, 2.03m, 2.85m, 4.27m, 5.14m, 5.73m, 6.46m, 5.55m, 4.35m, 2.44m, 1.34m, 0.98m], // Portland
        ["PA"] = [2.05m, 2.95m, 3.96m, 4.87m, 5.62m, 5.90m, 5.97m, 5.20m, 4.33m, 3.20m, 2.25m, 1.86m], // Philadelphia
        ["RI"] = [1.93m, 2.83m, 3.86m, 4.80m, 5.37m, 5.70m, 5.81m, 5.18m, 4.25m, 2.93m, 1.99m, 1.67m], // Providence
        ["SC"] = [2.87m, 3.64m, 4.84m, 6.07m, 6.63m, 6.43m, 6.17m, 5.56m, 4.70m, 3.95m, 3.23m, 2.62m], // Charleston
        ["SD"] = [1.61m, 2.46m, 3.73m, 5.04m, 5.70m, 6.28m, 6.69m, 5.56m, 4.50m, 2.97m, 2.00m, 1.48m], // Sioux Falls
        ["TN"] = [2.19m, 2.92m, 4.02m, 5.11m, 5.83m, 6.33m, 5.90m, 5.70m, 4.70m, 3.59m, 2.59m, 1.97m], // Nashville
        ["TX"] = [2.81m, 3.38m, 4.32m, 5.42m, 6.02m, 6.13m, 5.96m, 5.68m, 4.89m, 4.14m, 3.16m, 2.59m], // Houston
        ["UT"] = [1.97m, 2.82m, 4.33m, 5.54m, 6.83m, 7.49m, 7.61m, 6.56m, 5.36m, 3.75m, 2.35m, 1.74m], // Salt Lake City
        ["VT"] = [1.57m, 2.39m, 3.38m, 4.76m, 5.44m, 5.82m, 5.74m, 5.17m, 4.06m, 2.39m, 1.50m, 1.20m], // Burlington
        ["VA"] = [2.37m, 3.09m, 4.25m, 5.19m, 5.96m, 6.21m, 6.03m, 5.50m, 4.45m, 3.50m, 2.62m, 2.14m], // Virginia Beach
        ["WA"] = [0.98m, 1.94m, 2.75m, 4.51m, 5.34m, 5.99m, 6.38m, 5.48m, 4.03m, 2.11m, 1.12m, 0.84m], // Seattle
        ["WV"] = [1.95m, 2.62m, 3.68m, 4.77m, 5.57m, 6.07m, 5.72m, 5.43m, 4.41m, 3.21m, 2.25m, 1.74m], // Charleston
        ["WI"] = [1.67m, 2.55m, 3.68m, 4.82m, 5.56m, 6.21m, 6.16m, 5.23m, 4.39m, 2.80m, 1.83m, 1.44m], // Milwaukee
        ["WY"] = [2.28m, 3.16m, 4.37m, 5.52m, 6.30m, 7.08m, 6.90m, 5.96m, 5.14m, 3.58m, 2.50m, 1.93m], // Cheyenne
    };

    /// <summary>
    /// The peak sun hours for a month (1–12) in a state, by its two-letter code. Every state a lead or an installation
    /// can name is in the table, so an unknown one is a bug, not a case to handle.
    /// </summary>
    public static decimal For(string state, int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
        return ByState.TryGetValue(state, out var months)
            ? months[month - 1]
            : throw new ArgumentOutOfRangeException(nameof(state), state, "No peak sun hours are recorded for this state.");
    }

    /// <summary>Whether the table has a row for this state.</summary>
    public static bool Knows(string state) => ByState.ContainsKey(state);
}

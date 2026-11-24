using Application.DTOs.Common;

namespace Application.DTOs.Production;

/// <summary>
/// The fleet's last finished day (FR-P2): how many systems are at each health, and the ones doing worst. The day is
/// the last one that has ended in every zone the fleet stands in, so nothing is counted while it is still running.
/// <see cref="FleetMedianRatio"/> is what health was measured against (ADR 15); null when too few systems reported
/// that day, and each system was then judged against its own expected production alone.
/// </summary>
public sealed record FleetDayResponse(
    DateOnly Day,
    int Installations,
    int Reporting,
    decimal? FleetMedianRatio,
    IReadOnlyList<HealthCountResponse> Counts,
    IReadOnlyList<WorstPerformerResponse> Worst);

public sealed record HealthCountResponse(string Health, int Count);

/// <summary>One of the systems doing worst, lowest first: what it made against what it should have.</summary>
public sealed record WorstPerformerResponse(
    int InstallationId,
    AddressResponse Address,
    string CustomerName,
    decimal ActualKwh,
    decimal ExpectedKwh,
    decimal? Ratio,
    string Health);

/// <summary>One installation's month, day by day (FR-P3), in its own local days (BR-P4).</summary>
public sealed record MonthProductionResponse(
    int InstallationId,
    int Year,
    int Month,
    decimal ActualKwh,
    decimal ExpectedKwh,
    IReadOnlyList<DayProductionResponse> Days);

public sealed record DayProductionResponse(DateOnly Day, decimal ActualKwh, decimal ExpectedKwh);

/// <summary>One installation's year, month by month (FR-P4).</summary>
public sealed record YearProductionResponse(
    int InstallationId,
    int Year,
    decimal ActualKwh,
    decimal ExpectedKwh,
    IReadOnlyList<MonthProductionTotalResponse> Months);

public sealed record MonthProductionTotalResponse(int Month, decimal ActualKwh, decimal ExpectedKwh);

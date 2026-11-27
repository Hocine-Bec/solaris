namespace Application.Interfaces.Repositories;

/// <summary>Each installation's local days, added up from its readings (FR-P1): what the production views read.</summary>
public interface IEnergyProductionRepo
{
    /// <summary>One local day across every installation that has a row for it: what each made, from how many readings.</summary>
    Task<IReadOnlyList<(int InstallationId, int ActualWh, int ReadingCount)>> GetDayAsync(DateOnly day);

    /// <summary>One installation's days from <paramref name="from"/> up to, not including, <paramref name="to"/>.</summary>
    Task<IReadOnlyList<(DateOnly Day, int ActualWh, int ReadingCount)>> GetDaysAsync(int installationId, DateOnly from, DateOnly to);
}

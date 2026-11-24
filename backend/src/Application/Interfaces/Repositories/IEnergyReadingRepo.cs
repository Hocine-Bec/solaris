using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>
/// The readings the installations' systems send (BR-T1). Built naive on purpose (P4a's before): one question and one
/// insert per reading, where the fixes branch writes a whole batch in one statement.
/// </summary>
public interface IEnergyReadingRepo
{
    /// <summary>Whether this installation already has the slot: the duplicate check, asked once per reading.</summary>
    Task<bool> ExistsAsync(int installationId, DateTime recordedAt);

    void Add(EnergyReading reading);

    /// <summary>
    /// What each of these installations produced between two moments, by installation; an installation with no
    /// readings in the window isn't in the answer. Built naive (P4b's before): it sums the raw readings on every
    /// request, where the fixes branch reads one row per day from the rollups.
    /// </summary>
    Task<IReadOnlyList<(int InstallationId, decimal Kwh)>> SumByInstallationAsync(
        IReadOnlyCollection<int> installationIds, DateTime fromUtc, DateTime toUtc);

    /// <summary>
    /// One installation's readings between two moments, each with its slot, so the caller can put them in local days
    /// (BR-P4). Naive for the same reason: a month's view reads a month of raw readings.
    /// </summary>
    Task<IReadOnlyList<(DateTime RecordedAt, decimal Kwh)>> GetForAsync(int installationId, DateTime fromUtc, DateTime toUtc);
}

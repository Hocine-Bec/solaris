using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>The readings the installations' systems send (BR-T1), and what the production views read of them.</summary>
public interface IEnergyReadingRepo
{
    /// <summary>
    /// Stores a batch in one statement and marks the local days it touched (BR-P3), together: the installation and
    /// the slot are the table's key, so a slot it already has is passed over rather than stored twice, however many
    /// devices send it at the same moment. Answers how many rows were new.
    /// </summary>
    Task<int> AddBatchAsync(int installationId, string timeZone, IReadOnlyList<(DateTime SlotStart, int EnergyWh)> readings);
}

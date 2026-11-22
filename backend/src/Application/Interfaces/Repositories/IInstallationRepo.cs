using Application.Interfaces.Repositories.Base;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

public interface IInstallationRepo : IGenericRepo<Installation>
{
    /// <summary>The ID of the installation the lead's conversion created; null if it has none.</summary>
    Task<int?> GetIdForLeadAsync(int leadId);

    /// <summary>
    /// One page of the installations in these statuses (all when none is given), the last status change first, then
    /// by ID, with their address and customer, and how many match.
    /// </summary>
    Task<(IReadOnlyList<Installation> Items, int TotalCount)> GetPageAsync(IReadOnlyCollection<InstallationStatus> statuses,
        int skip, int take);

    /// <summary>
    /// One installation, tracked, with everything its page shows: its address, its customer, its assignments (their
    /// technicians, and who assigned and completed them) and its history (and who made each change).
    /// </summary>
    Task<Installation?> GetDetailsAsync(int id);

    /// <summary>The statuses of the customer's installations other than <paramref name="exceptId"/>, as stored (BR-C1).</summary>
    Task<IReadOnlyList<InstallationStatus>> GetStatusesOfCustomerAsync(int customerId, int exceptId);

    /// <summary>The installations this technician is assigned to, with their address, customer and assignments.</summary>
    Task<IReadOnlyList<Installation>> GetForTechnicianAsync(int technicianId);

    /// <summary>
    /// Saves the installation's row with the next save even when only its assignments changed, so the save checks its
    /// version and moves it (BR-I4).
    /// </summary>
    void MarkChanged(Installation installation);
}

using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InstallationRepo(AppDbContext context) : GenericRepo<Installation>(context), IInstallationRepo
{
    public async Task<int?> GetIdForLeadAsync(int leadId)
    {
        return await Context.Installations
            .Where(i => i.LeadId == leadId)
            .OrderBy(i => i.Id)
            .Select(i => (int?)i.Id)
            .FirstOrDefaultAsync();
    }

    public Task<(IReadOnlyList<Installation> Items, int TotalCount)> GetPageAsync(
        IReadOnlyCollection<InstallationStatus> statuses, int skip, int take)
    {
        var query = Context.Installations
            .Include(i => i.Address)
            .Include(i => i.Customer)
            .AsNoTracking()
            .Where(i => statuses.Count == 0 || statuses.Contains(i.Status))
            .OrderByDescending(i => i.StatusChangedAt).ThenByDescending(i => i.Id);
        return ToPageAsync(query, skip, take);
    }

    public Task<Installation?> GetDetailsAsync(int id) =>
        Context.Installations
            .Include(i => i.Address)
            .Include(i => i.Customer)
            .Include(i => i.Assignments).ThenInclude(a => a.Technician)
            .Include(i => i.Assignments).ThenInclude(a => a.AssignedBy)
            .Include(i => i.Assignments).ThenInclude(a => a.CompletedBy)
            .Include(i => i.History).ThenInclude(h => h.ChangedBy)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<IReadOnlyList<InstallationStatus>> GetStatusesOfCustomerAsync(int customerId, int exceptId) =>
        await Context.Installations
            .Where(i => i.CustomerId == customerId && i.Id != exceptId)
            .Select(i => i.Status)
            .ToListAsync();

    public async Task<IReadOnlyList<Installation>> GetForTechnicianAsync(int technicianId) =>
        await Context.Installations
            .Include(i => i.Address)
            .Include(i => i.Customer)
            .Include(i => i.Assignments.Where(a => a.TechnicianId == technicianId))
            .AsNoTracking()
            .Where(i => i.Assignments.Any(a => a.TechnicianId == technicianId))
            .AsSplitQuery()
            .ToListAsync();

    public void MarkChanged(Installation installation)
    {
        var entry = Context.Entry(installation);
        if (entry.State == EntityState.Unchanged)
            entry.State = EntityState.Modified;
    }
}

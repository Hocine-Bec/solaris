using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LeadRepo(AppDbContext context) : GenericRepo<Lead>(context), ILeadRepo
{
    public async Task<(IReadOnlyList<Lead> Items, int TotalCount)> GetPageAsync(IReadOnlyCollection<LeadStatus> statuses,
        int skip, int take)
    {
        // IDs grow with creation, so the highest ID is the newest lead.
        var query = Context.Leads
            .Include(x => x.Address)
            .AsNoTracking()
            .Where(x => statuses.Count == 0 || statuses.Contains(x.Status))
            .OrderByDescending(x => x.Id);
        return await ToPageAsync(query, skip, take);
    }

    public override async Task<Lead?> GetByIdAsync(int id)
    {
        return await Context.Leads
            .Include(x => x.Customer)
            .Include(x => x.Address)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<int> GetIdOfExistingAsync(string normalizedEmail)
    {
        return await Context.Leads
            .Where(x => x.NormalizedEmail == normalizedEmail)
            .OrderByDescending(x => x.Status == LeadStatus.New || x.Status == LeadStatus.Contacted)
            .ThenByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstAsync();
    }
}

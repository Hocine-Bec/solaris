using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LeadRepo(AppDbContext context) : GenericRepo<Lead>(context), ILeadRepo
{
    public async Task<(IReadOnlyList<Lead> Items, int TotalCount)> GetPageAsync(int skip, int take)
    {
        // IDs grow with creation, so the highest ID is the newest lead.
        var query = Context.Leads
            .Include(x => x.Customer)
            .Include(x => x.Address)
            .AsNoTracking()
            .OrderByDescending(x => x.Id);
        return await ToPageAsync(query, skip, take);
    }

    public override async Task<Lead?> GetByIdAsync(int id)
    {
        return await Context.Leads
            .Include(x => x.Customer)
            .Include(x => x.Address)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}

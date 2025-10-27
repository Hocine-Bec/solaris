using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LeadRepo(AppDbContext context) : GenericRepo<Lead>(context), ILeadRepo
{
    public override async Task<IReadOnlyList<Lead>> GetAllAsync()
    {
        return await Context.Leads
            .Include(x => x.Customer)
            .Include(x => x.Address)
            .AsNoTracking()
            .ToListAsync();
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
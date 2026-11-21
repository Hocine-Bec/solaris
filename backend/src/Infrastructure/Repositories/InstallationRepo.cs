using Application.Interfaces.Repositories;
using Domain.Entities;
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
}

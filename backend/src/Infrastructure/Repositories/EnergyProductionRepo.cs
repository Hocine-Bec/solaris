using Application.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EnergyProductionRepo(AppDbContext context) : IEnergyProductionRepo
{
    public async Task<IReadOnlyList<(int InstallationId, int ActualWh, int ReadingCount)>> GetDayAsync(DateOnly day)
    {
        var rows = await context.EnergyProduction.AsNoTracking()
            .Where(p => p.Day == day)
            .Select(p => new { p.InstallationId, p.ActualWh, p.ReadingCount })
            .ToListAsync();
        return rows.Select(r => (r.InstallationId, r.ActualWh, r.ReadingCount)).ToList();
    }

    public async Task<IReadOnlyList<(DateOnly Day, int ActualWh, int ReadingCount)>> GetDaysAsync(
        int installationId, DateOnly from, DateOnly to)
    {
        var rows = await context.EnergyProduction.AsNoTracking()
            .Where(p => p.InstallationId == installationId && p.Day >= from && p.Day < to)
            .OrderBy(p => p.Day)
            .Select(p => new { p.Day, p.ActualWh, p.ReadingCount })
            .ToListAsync();
        return rows.Select(r => (r.Day, r.ActualWh, r.ReadingCount)).ToList();
    }
}

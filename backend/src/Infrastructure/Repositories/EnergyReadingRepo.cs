using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EnergyReadingRepo(AppDbContext context) : IEnergyReadingRepo
{
    public Task<bool> ExistsAsync(int installationId, DateTime recordedAt) =>
        context.EnergyReadings.AnyAsync(r => r.InstallationId == installationId && r.RecordedAt == recordedAt);

    public void Add(EnergyReading reading) => context.EnergyReadings.Add(reading);

    public async Task<IReadOnlyList<(int InstallationId, decimal Kwh)>> SumByInstallationAsync(
        IReadOnlyCollection<int> installationIds, DateTime fromUtc, DateTime toUtc)
    {
        var sums = await context.EnergyReadings
            .Where(r => installationIds.Contains(r.InstallationId) && r.RecordedAt >= fromUtc && r.RecordedAt < toUtc)
            .GroupBy(r => r.InstallationId)
            .Select(g => new { g.Key, Kwh = g.Sum(r => r.EnergyKwh) })
            .ToListAsync();
        return sums.Select(s => (s.Key, s.Kwh)).ToList();
    }

    public async Task<IReadOnlyList<(DateTime RecordedAt, decimal Kwh)>> GetForAsync(int installationId, DateTime fromUtc, DateTime toUtc)
    {
        var rows = await context.EnergyReadings
            .Where(r => r.InstallationId == installationId && r.RecordedAt >= fromUtc && r.RecordedAt < toUtc)
            .OrderBy(r => r.RecordedAt)
            .Select(r => new { r.RecordedAt, r.EnergyKwh })
            .ToListAsync();
        return rows.Select(r => (r.RecordedAt, r.EnergyKwh)).ToList();
    }
}

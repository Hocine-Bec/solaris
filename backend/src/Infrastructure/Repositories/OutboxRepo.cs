using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class OutboxRepo(AppDbContext context) : IOutboxRepo
{
    public async Task<(IReadOnlyList<OutboxMessage> Items, int TotalCount)> GetFailedPageAsync(int skip, int take)
    {
        var failed = context.OutboxMessages.Where(m => m.Status == OutboxMessageStatus.Failed);
        var totalCount = await failed.CountAsync();
        // The ID last (time-ordered), so messages that failed at the same moment keep their place between pages
        var items = await failed.AsNoTracking()
            .Include(m => m.Lead)
            .Include(m => m.RetriedBy)
            .OrderByDescending(m => m.LastAttemptAt).ThenByDescending(m => m.Id)
            .Skip(skip).Take(take)
            .ToListAsync();
        return (items, totalCount);
    }

    public Task<bool> ExistsAsync(Guid id) => context.OutboxMessages.AnyAsync(m => m.Id == id);

    public Task<int> RetryFailedAsync(Guid? id, DateTime now, int retriedByUserId)
    {
        var failed = context.OutboxMessages.Where(m => m.Status == OutboxMessageStatus.Failed);
        if (id is { } one)
            failed = failed.Where(m => m.Id == one);
        var giveUpAt = now + OutboxMessage.DeliveryWindow;
        return failed.ExecuteUpdateAsync(s => s
            .SetProperty(m => m.Status, OutboxMessageStatus.Pending)
            .SetProperty(m => m.Attempts, 0)
            .SetProperty(m => m.NextAttemptAt, now)
            .SetProperty(m => m.GiveUpAt, giveUpAt)
            .SetProperty(m => m.RetriedAt, now)
            .SetProperty(m => m.RetriedByUserId, retriedByUserId));
    }
}

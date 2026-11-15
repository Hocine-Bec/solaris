using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>The outbox's messages, for the Admin's list of failed ones and their retries (FR-N2).</summary>
public interface IOutboxRepo
{
    /// <summary>One page of the Failed messages, newest failure first, with their lead and who last retried them.</summary>
    Task<(IReadOnlyList<OutboxMessage> Items, int TotalCount)> GetFailedPageAsync(int skip, int take);

    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Retries the Failed message with this id, or every Failed message when <paramref name="id"/> is null, in one
    /// statement: Pending, due now, its attempts back to 0, and another 24 hours from now; recording when and by whom.
    /// A message that isn't Failed at that moment is left as it is. Returns how many were retried.
    /// </summary>
    Task<int> RetryFailedAsync(Guid? id, DateTime now, int retriedByUserId);
}

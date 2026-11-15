using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Notifications;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Application.Services;

// A retry is one conditional update in PostgreSQL (only a row still Failed changes), so two Admins retrying the same
// email at once can't both succeed: one changes it, the other finds it no longer Failed.
public class NotificationService(IOutboxRepo outbox, IEmailService emails, TimeProvider time) : INotificationService
{
    public async Task<PagedResponse<FailedNotificationResponse>> GetFailedPageAsync(PageRequest request)
    {
        var (messages, totalCount) = await outbox.GetFailedPageAsync(request.Offset(), request.PageSize);
        var items = messages.Select(m =>
        {
            var lead = m.Lead!;
            var to = emails.RecipientOf(m.Type, lead);
            return new FailedNotificationResponse(m.Id, m.Type.ToString(), new RecipientResponse(to.Name, to.Address),
                new LeadSummary(lead.Id, $"{lead.FirstName} {lead.LastName}"), m.CreatedAt, m.LastAttemptAt,
                m.Attempts, m.LastError, m.RetriedAt, m.RetriedBy?.Name);
        }).ToList();
        return new PagedResponse<FailedNotificationResponse>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<Result> RetryAsync(Guid id, int actingUserId)
    {
        if (await outbox.RetryFailedAsync(id, Now(), actingUserId) == 1)
            return Result.Success();

        return await outbox.ExistsAsync(id)
            ? Result.Failure(NotificationErrors.NotFailed)
            : Result.Failure(NotificationErrors.NotFound(id));
    }

    public async Task<RetriedResponse> RetryAllAsync(int actingUserId) =>
        new(await outbox.RetryFailedAsync(null, Now(), actingUserId));

    private DateTime Now() => time.GetUtcNow().UtcDateTime;
}

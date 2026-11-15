using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Notifications;

namespace Application.Interfaces.Services;

/// <summary>The emails that failed for good, and retrying them (FR-N2, BR-N2).</summary>
public interface INotificationService
{
    Task<PagedResponse<FailedNotificationResponse>> GetFailedPageAsync(PageRequest request);

    /// <summary>Sends a Failed email again, with another 24 hours; <paramref name="actingUserId"/> is recorded.</summary>
    Task<Result> RetryAsync(Guid id, int actingUserId);

    /// <summary>Sends every email Failed at this moment again, each with another 24 hours.</summary>
    Task<RetriedResponse> RetryAllAsync(int actingUserId);
}

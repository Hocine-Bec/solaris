using Application.DTOs.Common;
using Application.DTOs.Notifications;
using Application.Errors;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>
/// The emails the outbox couldn't send in 24 hours of tries, and retrying them (FR-N2): the office's (Admin). A retry
/// gives an email another 24 hours; the dispatcher sends it within seconds.
/// </summary>
[Route("api/notifications")]
[Authorize(Policy = Policies.Admin)]
public class NotificationsController(INotificationService service) : ApiControllerBase
{
    [HttpGet("failed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<FailedNotificationResponse>>> GetFailed([FromQuery] PageRequest request)
    {
        return Ok(await service.GetFailedPageAsync(request));
    }

    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retry(Guid id)
    {
        if (CurrentUserId is not { } me)
            return Problem(AuthErrors.SignedOut);

        var result = await service.RetryAsync(id, me);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    /// <summary>Every email Failed at this moment, sent again: after an outage, they fail together.</summary>
    [HttpPost("failed/retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<RetriedResponse>> RetryAll()
    {
        if (CurrentUserId is not { } me)
            return Problem(AuthErrors.SignedOut);

        return Ok(await service.RetryAllAsync(me));
    }
}

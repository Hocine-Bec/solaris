using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Installations;
using Application.Errors;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>
/// Installations, for the office (FR-I1–I7): the list, one installation, its moves (BR-I1, BR-I2), its design and
/// time zone (FR-I4), and its assignments (FR-I5, FR-I6). Each change answers the installation as its page shows it.
/// The page sends <c>If-Match</c> with every change; this version ignores it (the fixes branch enforces it, BR-I4).
/// </summary>
[Route("api/installations")]
[Authorize(Policy = Policies.Admin)]
public class InstallationsController(IInstallationService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<InstallationSummaryResponse>>> GetPage([FromQuery] InstallationPageRequest request)
    {
        return Ok(await service.GetPageAsync(request));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstallationResponse>> GetById(int id) => Answer(await service.GetByIdAsync(id));

    [HttpPost("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<InstallationResponse>> Move(int id, [FromBody] MoveInstallationRequest request) =>
        AsActingUser(me => service.MoveAsync(id, request, me));

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<InstallationResponse>> Cancel(int id, [FromBody] CancelInstallationRequest request) =>
        AsActingUser(me => service.CancelAsync(id, request, me));

    [HttpPost("{id:int}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<InstallationResponse>> Deactivate(int id, [FromBody] DeactivateInstallationRequest request) =>
        AsActingUser(me => service.DeactivateAsync(id, request, me));

    [HttpPut("{id:int}/design")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InstallationResponse>> RecordDesign(int id, [FromBody] InstallationDesignRequest request) =>
        Answer(await service.RecordDesignAsync(id, request));

    [HttpPut("{id:int}/time-zone")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InstallationResponse>> SetTimeZone(int id, [FromBody] InstallationTimeZoneRequest request) =>
        Answer(await service.SetTimeZoneAsync(id, request));

    [HttpPost("{id:int}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<InstallationResponse>> Assign(int id, [FromBody] AssignTechnicianRequest request) =>
        AsActingUser(me => service.AssignAsync(id, request, me));

    [HttpDelete("{id:int}/assignments/{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InstallationResponse>> Unassign(int id, int assignmentId) =>
        Answer(await service.UnassignAsync(id, assignmentId));

    /// <summary>An Admin marks a technician's part complete for them (FR-I6); the assignment records who.</summary>
    [HttpPost("{id:int}/assignments/{assignmentId:int}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<InstallationResponse>> Complete(int id, int assignmentId) =>
        AsActingUser(me => service.CompleteAsync(id, assignmentId, me));

    private ActionResult<InstallationResponse> Answer(Result<InstallationResponse> result) =>
        result.IsSuccess ? Ok(result.Value) : Problem(result.Error);

    private async Task<ActionResult<InstallationResponse>> AsActingUser(Func<int, Task<Result<InstallationResponse>>> change) =>
        CurrentUserId is { } me ? Answer(await change(me)) : Problem(AuthErrors.SignedOut);
}

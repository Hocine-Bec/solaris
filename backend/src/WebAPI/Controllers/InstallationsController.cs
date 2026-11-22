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
/// One installation and every change answer its version as the <c>ETag</c>; every change must send it back as
/// <c>If-Match</c>: 428 without it, 409 when the installation has changed since (BR-I4).
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
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public Task<ActionResult<InstallationResponse>> Move(int id, [FromBody] MoveInstallationRequest request) =>
        AsActingUser((version, me) => service.MoveAsync(id, version, request, me));

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public Task<ActionResult<InstallationResponse>> Cancel(int id, [FromBody] CancelInstallationRequest request) =>
        AsActingUser((version, me) => service.CancelAsync(id, version, request, me));

    [HttpPost("{id:int}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public Task<ActionResult<InstallationResponse>> Deactivate(int id, [FromBody] DeactivateInstallationRequest request) =>
        AsActingUser((version, me) => service.DeactivateAsync(id, version, request, me));

    [HttpPut("{id:int}/design")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public Task<ActionResult<InstallationResponse>> RecordDesign(int id, [FromBody] InstallationDesignRequest request) =>
        Versioned(version => service.RecordDesignAsync(id, version, request));

    [HttpPut("{id:int}/time-zone")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public Task<ActionResult<InstallationResponse>> SetTimeZone(int id, [FromBody] InstallationTimeZoneRequest request) =>
        Versioned(version => service.SetTimeZoneAsync(id, version, request));

    [HttpPost("{id:int}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public Task<ActionResult<InstallationResponse>> Assign(int id, [FromBody] AssignTechnicianRequest request) =>
        AsActingUser((version, me) => service.AssignAsync(id, version, request, me));

    [HttpDelete("{id:int}/assignments/{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public Task<ActionResult<InstallationResponse>> Unassign(int id, int assignmentId) =>
        Versioned(version => service.UnassignAsync(id, version, assignmentId));

    /// <summary>An Admin marks a technician's part complete for them (FR-I6); the assignment records who.</summary>
    [HttpPost("{id:int}/assignments/{assignmentId:int}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public Task<ActionResult<InstallationResponse>> Complete(int id, int assignmentId) =>
        AsActingUser((version, me) => service.CompleteAsync(id, version, assignmentId, me));

    /// <summary>The installation, tagged with its version; or the failure.</summary>
    private ActionResult<InstallationResponse> Answer(Result<InstallationResponse> result)
    {
        if (!result.IsSuccess)
            return Problem(result.Error);
        Tag(result.Value.Version);
        return Ok(result.Value);
    }

    /// <summary>A change, made on the version its <c>If-Match</c> names: refused without one.</summary>
    private async Task<ActionResult<InstallationResponse>> Versioned(Func<uint, Task<Result<InstallationResponse>>> change) =>
        IfMatchVersion() is { } version ? Answer(await change(version)) : Problem(InstallationErrors.VersionRequired);

    /// <summary>As <see cref="Versioned"/>, for a change that records who made it.</summary>
    private Task<ActionResult<InstallationResponse>> AsActingUser(Func<uint, int, Task<Result<InstallationResponse>>> change) =>
        CurrentUserId is { } me
            ? Versioned(version => change(version, me))
            : Task.FromResult<ActionResult<InstallationResponse>>(Problem(AuthErrors.SignedOut));
}

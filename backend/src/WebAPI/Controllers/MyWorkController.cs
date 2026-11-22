using Application.Common.Models;
using Application.DTOs.Installations;
using Application.Errors;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>
/// A technician's own work (FR-I6): the installations they are assigned to, one of them, and marking their own part
/// complete. Another installation answers 404 to them, as if it didn't exist. One installation answers its version as
/// the <c>ETag</c>, and marking a part complete must send it back as <c>If-Match</c> (BR-I4).
/// </summary>
[Route("api/my-work")]
[Authorize(Policy = Policies.Technician)]
public class MyWorkController(IInstallationService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyWorkItemResponse>>> GetMine()
    {
        if (CurrentUserId is not { } me)
            return Problem(AuthErrors.SignedOut);
        return Ok(await service.GetMyWorkAsync(me));
    }

    [HttpGet("{installationId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyWorkInstallationResponse>> GetOne(int installationId)
    {
        if (CurrentUserId is not { } me)
            return Problem(AuthErrors.SignedOut);
        return Answer(await service.GetMyInstallationAsync(installationId, me));
    }

    [HttpPost("{installationId:int}/assignments/{assignmentId:int}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async Task<ActionResult<MyWorkInstallationResponse>> CompleteMine(int installationId, int assignmentId)
    {
        if (CurrentUserId is not { } me)
            return Problem(AuthErrors.SignedOut);
        if (IfMatchVersion() is not { } version)
            return Problem(InstallationErrors.VersionRequired);
        return Answer(await service.CompleteMineAsync(installationId, version, assignmentId, me));
    }

    /// <summary>The installation, tagged with its version; or the failure.</summary>
    private ActionResult<MyWorkInstallationResponse> Answer(Result<MyWorkInstallationResponse> result)
    {
        if (!result.IsSuccess)
            return Problem(result.Error);
        Tag(result.Value.Version);
        return Ok(result.Value);
    }
}

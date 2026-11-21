using Application.DTOs.Installations;
using Application.Errors;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>
/// A technician's own work (FR-I6): the installations they are assigned to, one of them, and marking their own part
/// complete. Another installation answers 404 to them, as if it didn't exist.
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
        var result = await service.GetMyInstallationAsync(installationId, me);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpPost("{installationId:int}/assignments/{assignmentId:int}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MyWorkInstallationResponse>> CompleteMine(int installationId, int assignmentId)
    {
        if (CurrentUserId is not { } me)
            return Problem(AuthErrors.SignedOut);
        var result = await service.CompleteMineAsync(installationId, assignmentId, me);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }
}

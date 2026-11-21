using Application.DTOs.Common;
using Application.DTOs.Installations;
using Application.DTOs.Users;
using Application.Errors;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>
/// Staff accounts, managed by the Admin (FR-U2, FR-U4): the list, one account, a new technician, a corrected name or
/// email, a new password, deactivating and reactivating. Each person changes their own password with
/// <c>PUT /api/auth/password</c>.
/// </summary>
[Route("api/users")]
[Authorize(Policy = Policies.Admin)]
public class UsersController(IUserService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetPage([FromQuery] PageRequest request)
    {
        return Ok(await service.GetPageAsync(request));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    /// <summary>A technician's account; the password is never returned, so the Admin's screen shows the one it sent.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
    {
        var result = await service.CreateTechnicianAsync(request);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var result = await service.UpdateAsync(id, request);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    [HttpPut("{id:int}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetPassword(int id, [FromBody] SetPasswordRequest request)
    {
        if (CurrentUserId is not { } me)
            return Problem(AuthErrors.SignedOut);

        var result = await service.SetPasswordAsync(me, id, request);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    [HttpPost("{id:int}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (CurrentUserId is not { } me)
            return Problem(AuthErrors.SignedOut);

        var result = await service.DeactivateAsync(me, id);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    /// <summary>A technician's assignments, open first, for their page (FR-U4); an empty list for anyone else.</summary>
    [HttpGet("{id:int}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserAssignmentResponse>>> GetAssignments(int id,
        [FromServices] IInstallationService installations)
    {
        return Ok(await installations.GetAssignmentsOfAsync(id));
    }

    [HttpPost("{id:int}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(int id)
    {
        var result = await service.ReactivateAsync(id);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }
}

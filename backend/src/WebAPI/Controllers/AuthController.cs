using Application.DTOs.Auth;
using Application.Errors;
using Application.Interfaces.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Security;

namespace WebAPI.Controllers;

/// <summary>
/// Staff sessions (FR-U1, ADR 7): signing in sets the session cookie, signing out clears it, and the staff app asks who
/// is signed in, as its code can't read the cookie.
/// </summary>
[Route("api/auth")]
public class AuthController(IAuthService service) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.PasswordChecks)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CurrentUserResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await service.SignInAsync(request);
        if (!result.IsSuccess)
            return Problem(result.Error);

        Response.Cookies.Append(Session.CookieName, result.Value.Token, CookieOptions(result.Value.ExpiresAt));
        return Ok(result.Value.User);
    }

    /// <summary>Clears the session cookie; also when the session had already ended, so it never fails.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(Session.CookieName, CookieOptions(expires: null));
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        if (CurrentUserId is not { } userId)
            return Problem(AuthErrors.SignedOut);

        var result = await service.GetCurrentUserAsync(userId);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    /// <summary>
    /// Changes your own password, given the current one. Your other sessions end; this one goes on with a new cookie,
    /// as its old token was issued before the change. A wrong current password is a field to fix (400), not a 401, so
    /// it doesn't end the session.
    /// </summary>
    [HttpPut("password")]
    [Authorize]
    [EnableRateLimiting(RateLimits.PasswordChecks)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (CurrentUserId is not { } userId)
            return Problem(AuthErrors.SignedOut);

        var result = await service.ChangePasswordAsync(userId, request);
        if (!result.IsSuccess)
            return Problem(result.Error);

        Response.Cookies.Append(Session.CookieName, result.Value.Token, CookieOptions(result.Value.ExpiresAt));
        return NoContent();
    }

    /// <summary>
    /// The session cookie: out of the page's scripts' reach (HttpOnly), sent only over HTTPS (browsers count
    /// localhost as secure) and only with the API's own requests from the same site (SameSite=Strict, Path=/api),
    /// lasting as long as its token, across closed windows.
    /// </summary>
    private static CookieOptions CookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api",
        Expires = expires,
        IsEssential = true
    };
}

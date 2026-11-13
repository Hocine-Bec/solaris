using System.Globalization;
using Application.DTOs.Auth;
using Application.Errors;
using Application.Interfaces.Services;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        if (!int.TryParse(User.FindFirst(Session.UserIdClaim)?.Value, NumberStyles.None, CultureInfo.InvariantCulture,
                out var userId))
            return Problem(AuthErrors.SignedOut);

        var result = await service.GetCurrentUserAsync(userId);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
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

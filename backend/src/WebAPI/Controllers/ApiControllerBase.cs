using System.Diagnostics;
using System.Globalization;
using Application.Common.Models;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// The base of every API controller. Each action picks its own success status; an expected failure becomes a
/// ProblemDetails response through <see cref="Problem(Error)"/> (ADR 3).
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// The ProblemDetails response for an expected failure: the status from its kind, plus its stable code. An invalid
    /// field is answered as validation answers one: 400 <c>validation_failed</c>, with the message under the field.
    /// </summary>
    protected ObjectResult Problem(Error error)
    {
        if (error.Kind == ErrorKind.Invalid)
        {
            ModelState.AddModelError(error.Field ?? string.Empty, error.Message);
            return (ObjectResult)ValidationProblem(modelStateDictionary: ModelState);
        }

        var result = Problem(detail: error.Message, statusCode: StatusCodeFor(error.Kind));
        ((ProblemDetails)result.Value!).Extensions["code"] = error.Code;
        return result;
    }

    /// <summary>The signed-in account's ID, from its session token; null if the token has none.</summary>
    protected int? CurrentUserId =>
        int.TryParse(User.FindFirst(Session.UserIdClaim)?.Value, NumberStyles.None, CultureInfo.InvariantCulture,
            out var id)
            ? id
            : null;

    private static int StatusCodeFor(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        _ => throw new UnreachableException($"No HTTP status for the error kind {kind}.")
    };
}

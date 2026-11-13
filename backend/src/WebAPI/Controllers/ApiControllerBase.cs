using System.Diagnostics;
using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// The base of every API controller. Each action picks its own success status; an expected failure becomes a
/// ProblemDetails response through <see cref="Problem(Error)"/> (ADR 3).
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The ProblemDetails response for an expected failure: the status from its kind, plus its stable code.</summary>
    protected ObjectResult Problem(Error error)
    {
        var result = Problem(detail: error.Message, statusCode: StatusCodeFor(error.Kind));
        ((ProblemDetails)result.Value!).Extensions["code"] = error.Code;
        return result;
    }

    private static int StatusCodeFor(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        _ => throw new UnreachableException($"No HTTP status for the error kind {kind}.")
    };
}

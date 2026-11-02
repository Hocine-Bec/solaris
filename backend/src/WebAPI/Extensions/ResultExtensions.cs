using Application.Common.Models;
using Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Extensions;

/// <summary>
/// Extension methods for converting results to action results.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an <see cref="IActionResult"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="actionName">The name of the action.</param>
    /// <param name="routeValues">The route values.</param>
    /// <returns>An <see cref="IActionResult"/>.</returns>
    public static ActionResult HandleResult<T>(this Result<T> result, string? actionName = null, object? routeValues = null)
    {
        return result.StatusCode switch
        {
            ResultStatusCode.Success => new OkObjectResult(result.Value),
            ResultStatusCode.Created => new CreatedAtActionResult(actionName, null, routeValues, result.Value),
            ResultStatusCode.NoContent => new NoContentResult(),
            ResultStatusCode.BadRequest => new BadRequestObjectResult(result.Error),
            ResultStatusCode.Unauthorized => new UnauthorizedResult(),
            ResultStatusCode.Forbidden => new ForbidResult(),
            ResultStatusCode.NotFound => new NotFoundObjectResult(result.Error),
            ResultStatusCode.Conflict => new ConflictObjectResult(result.Error),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }

    /// <summary>
    /// Converts a <see cref="Result"/> to an <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>An <see cref="IActionResult"/>.</returns>
    public static ActionResult HandleResult(this Result result)
    {
        return result.StatusCode switch
        {
            ResultStatusCode.Success => new OkResult(),
            ResultStatusCode.Created => new StatusCodeResult(StatusCodes.Status201Created),
            ResultStatusCode.NoContent => new NoContentResult(),
            ResultStatusCode.BadRequest => new BadRequestObjectResult(result.Error),
            ResultStatusCode.NotFound => new NotFoundObjectResult(result.Error),
            ResultStatusCode.Forbidden => new ForbidResult(),
            ResultStatusCode.Unauthorized => new UnauthorizedResult(), 
            ResultStatusCode.Conflict => new ConflictObjectResult(result.Error),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }
}
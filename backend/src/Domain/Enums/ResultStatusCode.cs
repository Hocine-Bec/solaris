namespace Domain.Enums;

/// <summary>
/// Represents the status code of a result.
/// </summary>
public enum ResultStatusCode
{
    /// <summary>
    /// The operation was successful.
    /// </summary>
    Success = 200,
    /// <summary>
    /// The resource was created.
    /// </summary>
    Created = 201,
    /// <summary>
    /// The operation was successful but there is no content to return.
    /// </summary>
    NoContent = 204,
    /// <summary>
    /// The request was invalid.
    /// </summary>
    BadRequest = 400,
    /// <summary>
    /// The user is not authorized.
    /// </summary>
    Unauthorized = 401,
    /// <summary>
    /// The user is forbidden from accessing the resource.
    /// </summary>
    Forbidden = 403,
    /// <summary>
    /// The resource was not found.
    /// </summary>
    NotFound = 404,
    /// <summary>
    /// The resource already exists
    /// </summary>
    Conflict = 409,
    /// <summary>
    /// An internal server error occurred.
    /// </summary>
    InternalServerError = 500,
}
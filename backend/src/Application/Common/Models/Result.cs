using Domain.Enums;

namespace Application.Common.Models;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }
    /// <summary>
    /// Gets the status code of the result.
    /// </summary>
    public ResultStatusCode StatusCode { get; }
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">A value indicating whether the operation was successful.</param>
    /// <param name="statusCode">The status code of the result.</param>
    /// <param name="error">The error message.</param>
    protected Result(bool isSuccess, ResultStatusCode statusCode, string error)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Error = error;
    }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    /// <param name="statusCode">The status code of the result.</param>
    /// <returns>A success result.</returns>
    public static Result Success(ResultStatusCode statusCode = ResultStatusCode.Success) => new(true, statusCode, string.Empty);
    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="statusCode">The status code of the result.</param>
    /// <returns>A failure result.</returns>
    public static Result Failure(string error, ResultStatusCode statusCode = ResultStatusCode.BadRequest) => new(false, statusCode, error);
    /// <summary>
    /// Creates a success result with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="statusCode">The status code of the result.</param>
    /// <returns>A success result with a value.</returns>
    public static Result<T> Success<T>(T value, ResultStatusCode statusCode = ResultStatusCode.Success) => new(value, true, statusCode, string.Empty);
    /// <summary>
    /// Creates a failure result with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="error">The error message.</param>
    /// <param name="statusCode">The status code of the result.</param>
    /// <returns>A failure result with a value.</returns>
    public static Result<T> Failure<T>(string error, ResultStatusCode statusCode = ResultStatusCode.BadRequest) => new(default!, false, statusCode, error);
    /// <summary>
    /// Creates a not found result.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="error">The error message.</param>
    /// <returns>A not found result.</returns>
    public static Result<T> NotFound<T>(string error) => new(default!, false, ResultStatusCode.NotFound, error);
    /// <summary>
    /// Creates a not found result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A not found result.</returns>
    public static Result NotFound(string error) => new(false, ResultStatusCode.NotFound, error);
    /// <summary>
    /// Creates an unauthorized result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>An unauthorized result.</returns>
    public static Result Unauthorized(string error) => new(false, ResultStatusCode.Unauthorized, error);
    /// <summary>
    /// Creates a no content result.
    /// </summary>
    /// <returns>A no content result.</returns>
    public static Result NoContent() => new(true, ResultStatusCode.NoContent, string.Empty);
}

/// <summary>
/// Represents the result of an operation with a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="isSuccess">A value indicating whether the operation was successful.</param>
    /// <param name="statusCode">The status code of the result.</param>
    /// <param name="error">The error message.</param>
    protected internal Result(T value, bool isSuccess, ResultStatusCode statusCode, string error)
        : base(isSuccess, statusCode, error)
    {
        if (isSuccess && value == null)
            throw new ArgumentNullException(nameof(value), "Value cannot be null for a successful result.");
        Value = value;
    }
}
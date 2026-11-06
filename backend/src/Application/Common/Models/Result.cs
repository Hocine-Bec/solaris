using System.Diagnostics.CodeAnalysis;

namespace Application.Common.Models;

/// <summary>
/// The outcome of an operation that can fail in an expected way: a success, or a failure carrying an
/// <see cref="Models.Error"/>. Unexpected failures are exceptions, handled once by the API (ADR 3).
/// </summary>
public class Result
{
  protected Result(Error? error) => Error = error;
  /// <summary>Why the operation failed; <c>null</c> when it succeeded.</summary>
  public Error? Error { get; }
  [MemberNotNullWhen(false, nameof(Error))]
  public bool IsSuccess => Error is null;
  public static Result Success() => new(null);
  public static Result Failure(Error error) => new(error);
  public static Result<T> Success<T>(T value) => new(value, null);
  public static Result<T> Failure<T>(Error error) => new(default, error);
}

/// <summary>A <see cref="Result"/> whose success carries a value.</summary>
public sealed class Result<T> : Result
{
  private readonly T? _value;

  internal Result(T? value, Error? error) : base(error)
  {
    if (error is null && value is null)
      throw new ArgumentNullException(nameof(value), "A successful result needs a value.");
    _value = value;
  }

  /// <summary>The value of a success. Reading it from a failure is a bug, so it throws.</summary>
  public T Value => IsSuccess
      ? _value!
      : throw new InvalidOperationException($"A failed result has no value ({Error.Code}).");
}

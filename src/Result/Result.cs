namespace Davish.Result;

/// <summary>
/// Represents the outcome of an operation as either a success or a failure carrying an <see cref="Error"/>.
/// </summary>
public class Result
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess => Error == Error.None;

    /// <summary>Gets the error. Equals <see cref="Error.None"/> when the result is successful.</summary>
    public Error Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class. Exposed to derived classes so that
    /// application-specific result types can be built on top of this one.
    /// </summary>
    /// <param name="error">The error describing the failure, or <see cref="Error.None"/> for a success.</param>
    protected Result(Error error)
    {
        Error = error;
    }

    /// <summary>Creates a successful result.</summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new(Error.None);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(Error error) => error == Error.None
                  ? throw new InvalidResultStateException(false, error)
                  : new(error);

    /// <summary>Creates a successful result carrying a value.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value produced by the operation.</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Success<TValue>(TValue value) where TValue : notnull
        => new(value, Error.None);

    /// <summary>Creates a failed result of the specified value type.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Failure<TValue>(Error error) where TValue : notnull
        => error == Error.None
                  ? throw new InvalidResultStateException(false, error)
                  : new(default, error);
}

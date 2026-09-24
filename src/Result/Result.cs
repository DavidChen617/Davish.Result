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

    /// <summary>Deconstructs this result into its success flag and error.</summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error. Equals <see cref="Error.None"/> when <paramref name="isSuccess"/> is <see langword="true"/>.</param>
    public void Deconstruct(out bool isSuccess, out Error error)
    {
        isSuccess = IsSuccess;
        error = Error;
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

    /// <summary>
    /// Creates a failed result from a caught exception. See <see cref="ExceptionalError.From(Exception)"/> for
    /// the safety warning about <see cref="Error.Description"/> exposing the raw exception message.
    /// </summary>
    /// <param name="exception">The exception describing the failure.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(Exception exception) => Failure(ExceptionalError.From(exception));

    /// <summary>
    /// Creates a failed result from a caught exception, wrapped as the cause of a client-safe
    /// <paramref name="error"/> you control. See <see cref="ExceptionalError.From(Exception, Error)"/>.
    /// </summary>
    /// <param name="exception">The exception describing the failure.</param>
    /// <param name="error">The client-safe error to surface; the exception becomes its cause.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(Exception exception, Error error) => Failure(ExceptionalError.From(exception, error));

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

    /// <summary>
    /// Creates a failed result of the specified value type from a caught exception. See
    /// <see cref="ExceptionalError.From(Exception)"/> for the safety warning about <see cref="Error.Description"/>
    /// exposing the raw exception message.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="exception">The exception describing the failure.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Failure<TValue>(Exception exception) where TValue : notnull
        => Failure<TValue>(ExceptionalError.From(exception));

    /// <summary>
    /// Creates a failed result of the specified value type from a caught exception, wrapped as the cause of a
    /// client-safe <paramref name="error"/> you control. See <see cref="ExceptionalError.From(Exception, Error)"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="exception">The exception describing the failure.</param>
    /// <param name="error">The client-safe error to surface; the exception becomes its cause.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Failure<TValue>(Exception exception, Error error) where TValue : notnull
        => Failure<TValue>(ExceptionalError.From(exception, error));
}

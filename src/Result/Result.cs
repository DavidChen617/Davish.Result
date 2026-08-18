namespace Davish.Result;

/// <summary>
/// Represents the outcome of an operation as either a success or a failure carrying an <see cref="Error"/>.
/// </summary>
public class Result
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the error. Equals <see cref="Error.None"/> when the result is successful.</summary>
    public Error Error { get; }

    internal Result(bool isSuccess, Error error)
    {
        if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None))
            throw new InvalidResultStateException(isSuccess, error);

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Creates a successful result.</summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Creates a successful result carrying a value.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value produced by the operation.</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Success<TValue>(TValue value) where TValue : notnull
        => new(value, true, Error.None);

    /// <summary>Creates a failed result of the specified value type.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    public static Result<TValue> Failure<TValue>(Error error) where TValue : notnull
        => new(default, false, error);
}

/// <summary>
/// Represents the outcome of an operation that produces a value of type <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TValue">The type of the produced value.</typeparam>
public sealed class Result<TValue> : Result where TValue : notnull
{
    private readonly TValue? _value;

    /// <summary>
    /// Gets the produced value.
    /// </summary>
    /// <exception cref="ResultException">Thrown when the result is not successful.</exception>
    public TValue Value => IsSuccess ? _value! : throw new ResultValueUnavailableException(Error);

    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
        => _value = value;

    /// <summary>
    /// Implicitly converts a value into a result: a successful result when non-null,
    /// otherwise a failure with <see cref="Error.NullValue"/>.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    /// <summary>
    /// Implicitly converts an <see cref="Error"/> into a failed <see cref="Result{TValue}"/>.
    /// </summary>
    /// <param name="error">The error to wrap.</param>
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}

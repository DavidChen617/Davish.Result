namespace Davish.Result;

/// <summary>
/// Represents the outcome of an operation that produces a value of type <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TValue">The type of the produced value.</typeparam>
public class Result<TValue> : Result where TValue : notnull
{
    private readonly TValue? _value;

    /// <summary>
    /// Gets the produced value.
    /// </summary>
    /// <exception cref="ResultException">Thrown when the result is not successful.</exception>
    public TValue Value => IsSuccess ? _value! : throw new ResultValueUnavailableException(Error);

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TValue}"/> class. Exposed to derived classes so that
    /// application-specific result types can be built on top of this one; also usable from within this assembly
    /// so the base <see cref="Davish.Result.Result"/> factory methods can construct instances directly.
    /// </summary>
    /// <param name="value">The value produced by the operation, or <see langword="null"/> for a failure.</param>
    /// <param name="error">The error describing the failure, or <see cref="Error.None"/> for a success.</param>
    protected internal Result(TValue? value, Error error) : base(error)
    {
        if (error == Error.None && value is null)
            throw new InvalidResultStateException(true, error);

        _value = value;
    }

    /// <summary>
    /// Deconstructs this result into its success flag, value, and error. <paramref name="value"/> reads the
    /// underlying field directly rather than <see cref="Value"/>, so it never throws on failure — it is
    /// <see langword="default"/> when <paramref name="isSuccess"/> is <see langword="false"/>.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="value">The produced value, or <see langword="default"/> on failure.</param>
    /// <param name="error">The error. Equals <see cref="Error.None"/> when <paramref name="isSuccess"/> is <see langword="true"/>.</param>
    public void Deconstruct(out bool isSuccess, out TValue? value, out Error error)
    {
        isSuccess = IsSuccess;
        value = _value;
        error = Error;
    }

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

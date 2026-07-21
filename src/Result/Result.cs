namespace Davish.Result;

/// <summary>
/// Base class for error categories, carrying a stable, human-readable name.
/// </summary>
public abstract class ErrorTypeBase
{
    /// <summary>
    /// Gets the name that identifies this error type.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name that identifies the error type.</param>
    protected ErrorTypeBase(string name) => Name = name;
}

/// <summary>
/// Categorizes an <see cref="Error"/>. Provides a set of built-in types and can be
/// extended to define application-specific error categories.
/// </summary>
/// <param name="name">The name that identifies the error type.</param>
public class ErrorType(string name) : ErrorTypeBase(name)
{
    /// <summary>Represents the absence of an error (used by successful results).</summary>
    public static readonly ErrorType None = new(nameof(None));

    /// <summary>Represents an error caused by a <see langword="null"/> value.</summary>
    public static readonly ErrorType NullValue = new(nameof(NullValue));

    /// <summary>Represents a validation error.</summary>
    public static readonly ErrorType Validation = new(nameof(Validation));

    /// <summary>Represents a resource-not-found error.</summary>
    public static readonly ErrorType NotFound = new(nameof(NotFound));

    /// <summary>Represents a malformed or invalid request error.</summary>
    public static readonly ErrorType BadRequest = new(nameof(BadRequest));

    /// <summary>Represents an authentication error (the caller is not authenticated).</summary>
    public static readonly ErrorType Unauthorized = new(nameof(Unauthorized));

    /// <summary>Represents an authorization error (the caller is authenticated but not allowed).</summary>
    public static readonly ErrorType Forbidden = new(nameof(Forbidden));

    /// <summary>Represents a conflict, such as a duplicate or a concurrency violation.</summary>
    public static readonly ErrorType Conflict = new(nameof(Conflict));

    /// <summary>Represents an unexpected, unhandled error.</summary>
    public static readonly ErrorType Unexpected = new(nameof(Unexpected));

    /// <summary>Represents a service-unavailable error.</summary>
    public static readonly ErrorType ServiceUnavailable = new(nameof(ServiceUnavailable));
}

/// <summary>
/// Represents an error with a code, a description, a category, and optional per-field messages.
/// </summary>
public sealed record Error
{
    /// <summary>Gets the machine-readable error code.</summary>
    public string Code { get; }

    /// <summary>Gets the human-readable error description.</summary>
    public string Description { get; }

    /// <summary>Gets the category of this error. Defaults to <see cref="ErrorType.Validation"/>.</summary>
    public ErrorType Type { get; } = ErrorType.Validation;

    /// <summary>
    /// Gets the per-field error messages, keyed by field name. Typically used for validation errors.
    /// </summary>
    public Dictionary<string, List<string>> Fields { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> record.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">The human-readable error description.</param>
    public Error(string code, string description)
    {
        Code = code;
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> record with a specific category.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">The human-readable error description.</param>
    /// <param name="type">The error category.</param>
    public Error(string code, string description, ErrorType type) : this(code, description) => Type = type;

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> record with per-field messages.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">The human-readable error description.</param>
    /// <param name="fields">The per-field error messages, keyed by field name.</param>
    public Error(string code, string description, Dictionary<string, List<string>> fields) : this(code, description) =>
        Fields = fields;

    /// <summary>Gets the canonical "no error" instance, used by successful results.</summary>
    public static Error None { get; } = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>Gets the canonical error for a <see langword="null"/> value.</summary>
    public static Error NullValue { get; } = new("Null.Value", "Null value was provided", ErrorType.NullValue);

    /// <summary>
    /// Implicitly converts an <see cref="Error"/> into a failed <see cref="Result"/>.
    /// </summary>
    /// <param name="error">The error to wrap.</param>
    public static implicit operator Result(Error error) => Result.Failure(error);

    /// <summary>
    /// Adds a single error message for the specified field. Creates the field entry if it does not exist yet.
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <param name="message">The error message to add.</param>
    /// <returns>The same <see cref="Error"/> instance, to allow chaining.</returns>
    public Error AddFieldError(string field, string message)
    {
        if (!Fields.TryGetValue(field, out var messages))
            Fields[field] = messages = new();

        messages.Add(message);

        return this;
    }

    /// <summary>
    /// Adds multiple error messages for the specified field. Creates the field entry if it does not exist yet.
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <param name="messages">The error messages to add.</param>
    /// <returns>The same <see cref="Error"/> instance, to allow chaining.</returns>
    public Error AddFieldError(string field, ICollection<string> messages)
    {
        foreach (var message in messages)
            AddFieldError(field, message);

        return this;
    }
}

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
            throw new InvalidOperationException("One or more errors have been provided.");

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
    /// <exception cref="InvalidOperationException">Thrown when the result is not successful.</exception>
    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("The result is not successful.");

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

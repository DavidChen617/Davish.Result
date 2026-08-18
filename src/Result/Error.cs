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
public class ErrorType : ErrorTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorType"/> class. Not directly constructible: define an
    /// application-specific error category by subclassing <see cref="ErrorType"/> with a
    /// <see langword="static readonly"/> singleton field, following the pattern used by the built-in types below.
    /// </summary>
    /// <param name="name">The name that identifies the error type.</param>
    protected ErrorType(string name) : base(name)
    {
    }

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
    public ErrorTypeBase Type { get; } = ErrorType.Validation;

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
    public Error(string code, string description, ErrorTypeBase type) : this(code, description) => Type = type;

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

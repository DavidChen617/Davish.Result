using System.Text.Json.Serialization;

namespace Davish.Result;

/// <summary>
/// Categorizes an <see cref="Error"/> by name. Provides a set of built-in categories and can be extended by
/// declaring additional <see langword="static readonly"/> instances with application-specific names.
/// </summary>
[JsonConverter(typeof(ErrorTypeJsonConverter))]
public readonly record struct ErrorType
{
    /// <summary>Gets the name that identifies this error category.</summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorType"/> struct.
    /// </summary>
    /// <param name="value">The name that identifies this error category.</param>
    /// <exception cref="InvalidErrorTypeException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/> or empty.
    /// </exception>
    public ErrorType(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new InvalidErrorTypeException(value);

        Value = value;
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

    /// <summary>Returns <see cref="Value"/>.</summary>
    public override string ToString() => Value;
}

namespace Davish.Result;

/// <summary>
/// Thrown when an <see cref="ErrorType"/> is constructed with a <see langword="null"/> or empty name.
/// </summary>
public sealed class InvalidErrorTypeException : ResultException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidErrorTypeException"/> class.
    /// </summary>
    /// <param name="value">The invalid value that was provided.</param>
    public InvalidErrorTypeException(string? value)
        : base($"Error type name cannot be null or empty, but got: '{value}'.")
    {
    }
}

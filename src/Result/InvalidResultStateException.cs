namespace Davish.Result;

/// <summary>
/// Thrown when a <see cref="Result"/> is constructed with an inconsistent success/error combination:
/// a success carrying a non-<see cref="Error.None"/> error, or a failure carrying <see cref="Error.None"/>.
/// </summary>
public sealed class InvalidResultStateException : ResultException
{
    /// <summary>Gets whether the result claimed to be successful.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the error that was provided.</summary>
    public Error Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidResultStateException"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the result claimed to be successful.</param>
    /// <param name="error">The error that was provided.</param>
    public InvalidResultStateException(bool isSuccess, Error error)
        : base(isSuccess
            ? $"A successful result cannot carry an error, but got: {error.Code} - {error.Description}"
            : "A failed result must carry an error, but Error.None was provided.")
    {
        IsSuccess = isSuccess;
        Error = error;
    }
}

namespace Davish.Result;

/// <summary>
/// Thrown when <see cref="Result{TValue}.Value"/> is accessed on a failed result.
/// </summary>
public sealed class ResultValueUnavailableException : ResultException
{
    /// <summary>Gets the error that caused the failure.</summary>
    public Error Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultValueUnavailableException"/> class.
    /// </summary>
    /// <param name="error">The error that caused the failure.</param>
    public ResultValueUnavailableException(Error error)
        : base($"The result is not successful: {error.Code} - {error.Description}")
    {
        Error = error;
    }
}

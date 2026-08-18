namespace Davish.Result;

/// <summary>
/// Base class for exceptions thrown by this library when something is used or configured in an invalid state.
/// </summary>
public abstract class ResultException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    protected ResultException(string message) : base(message)
    {
    }
}

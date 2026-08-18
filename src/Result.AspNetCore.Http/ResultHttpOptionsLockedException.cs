namespace Davish.Result;

/// <summary>
/// Thrown when the error-type-to-status-code mapping is configured after it has already started
/// resolving status codes for requests.
/// </summary>
public sealed class ResultHttpOptionsLockedException : ResultException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultHttpOptionsLockedException"/> class.
    /// </summary>
    public ResultHttpOptionsLockedException()
        : base("ResultHttpOptions cannot be reconfigured after status codes have started being resolved. " +
               "Configure it once at startup, before the app begins handling requests.")
    {
    }
}

namespace Davish.Result;

/// <summary>
/// An <see cref="Error"/> built from a caught <see cref="System.Exception"/>, preserving it (and its
/// <see cref="System.Exception.InnerException"/> chain, mapped onto <see cref="Error.Causes"/>) for diagnostics
/// such as structured logging.
/// </summary>
/// <remarks>
/// <b>Warning:</b> <see cref="Error.Description"/> on an <see cref="ExceptionalError"/> built via
/// <see cref="From(System.Exception)"/> is the raw <see cref="System.Exception.Message"/> — it may contain
/// implementation details (SQL fragments, file paths, internal type/library names) that should <b>not</b> be
/// exposed to an untrusted client in production. If this <see cref="Error"/> could reach an HTTP response, use
/// <see cref="From(System.Exception, Error)"/> instead: it wraps this as the <see cref="Error.InnerError"/> of a
/// curated, client-safe <see cref="Error"/> you control, keeping the raw exception message out of anything sent
/// back to the caller.
/// </remarks>
public sealed record ExceptionalError : Error
{
    /// <summary>Gets the original exception this error was built from.</summary>
    public Exception Exception { get; }

    private ExceptionalError(Exception exception)
        : base(exception.GetType().Name, exception.Message, ErrorType.Unexpected)
    {
        Exception = exception;
    }

    /// <summary>
    /// Wraps <paramref name="exception"/> as an <see cref="ExceptionalError"/>, using its type name as
    /// <see cref="Error.Code"/> and its message as <see cref="Error.Description"/>. If <paramref name="exception"/>
    /// has an <see cref="System.Exception.InnerException"/>, it is recursively wrapped and added as a cause (see
    /// <see cref="Error.CausedBy(Error)"/>), mirroring the .NET inner-exception chain.
    /// </summary>
    /// <param name="exception">The exception to wrap.</param>
    public static ExceptionalError From(Exception exception)
    {
        var error = new ExceptionalError(exception);
        return exception.InnerException is { } inner
            ? (ExceptionalError)error.CausedBy(From(inner))
            : error;
    }

    /// <summary>
    /// Wraps <paramref name="exception"/> as the <see cref="Error.InnerError"/> of <paramref name="error"/>. Use
    /// this when the error could reach an untrusted client: <paramref name="error"/> (which you control) is what
    /// gets exposed, and the raw exception — including its message — only lives in <see cref="Error.InnerError"/>.
    /// </summary>
    /// <param name="exception">The exception to wrap.</param>
    /// <param name="error">The client-safe error to surface; the exception becomes its cause.</param>
    public static Error From(Exception exception, Error error) => error.CausedBy(From(exception));
}

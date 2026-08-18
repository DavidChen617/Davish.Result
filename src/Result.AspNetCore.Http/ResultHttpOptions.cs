using System.Collections.Concurrent;
using static Davish.Result.ErrorType;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Davish.Result;

/// <summary>
/// Configures the HTTP status code that <see cref="ResultToMinimalResultExtension"/> maps each <see cref="ErrorType"/> to.
/// Not directly accessible outside this assembly — configure it via
/// <c>IServiceCollection.AddCustomResultErrorTypeMap(...)</c>.
/// </summary>
/// <remarks>
/// Error types are matched by reference, not by <see cref="ErrorTypeBase.Name"/>: two distinct
/// <see cref="ErrorTypeBase"/> instances that happen to share the same <c>Name</c> (a "lookalike") are treated
/// as different categories and must be registered separately. Define each category once as a
/// <see langword="static readonly"/> field (as <see cref="ErrorType"/> itself does) and reuse that same instance
/// both when constructing an <see cref="Error"/> and when mapping it here.
/// </remarks>
internal static class ResultHttpOptions
{
    private static readonly IReadOnlyDictionary<ErrorTypeBase, int> DefaultStatusCodesByErrorType = new Dictionary<ErrorTypeBase, int>
    {
        [Validation] = Status400BadRequest,
        [NullValue] = Status400BadRequest,
        [NotFound] = Status404NotFound,
        [BadRequest] = Status400BadRequest,
        [Unauthorized] = Status401Unauthorized,
        [Forbidden] = Status403Forbidden,
        [Conflict] = Status409Conflict,
        [Unexpected] = Status500InternalServerError,
        [ServiceUnavailable] = Status503ServiceUnavailable
    };

    private static ConcurrentDictionary<ErrorTypeBase, int> s_statusCodesByErrorType = new(DefaultStatusCodesByErrorType);

    /// <summary>
    /// Whether <see cref="ResolveStatusCode"/> has resolved at least one status code. Once <see langword="true"/>,
    /// <see cref="Configure"/> throws — the configuration is frozen the moment it starts being used, the same way
    /// <c>JsonSerializerOptions</c> and <c>ServiceCollection</c> freeze after first use / <c>Build()</c>.
    /// </summary>
    private static volatile bool s_locked;

    /// <summary>
    /// Replaces the current configuration wholesale. Intended to be called once at startup (e.g. from <c>Program.cs</c>),
    /// before the app begins resolving status codes for real requests.
    /// </summary>
    /// <param name="configure">Builds the new configuration: whether to seed the built-in mappings, and any custom ones.</param>
    /// <exception cref="ResultHttpOptionsLockedException">
    /// Thrown when this is called after <see cref="ResolveStatusCode"/> has already resolved at least once.
    /// </exception>
    internal static void Configure(Action<ResultHttpOptionsBuilder> configure)
    {
        if (s_locked)
            throw new ResultHttpOptionsLockedException();

        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ResultHttpOptionsBuilder();
        configure(builder);

        var map = new ConcurrentDictionary<ErrorTypeBase, int>();

        if (builder.UseDefault)
            foreach (var (errorType, statusCode) in DefaultStatusCodesByErrorType)
                map[errorType] = statusCode;

        if (builder.CustomMap is not null)
            foreach (var (errorType, statusCode) in builder.CustomMap)
                map[errorType] = statusCode;

        s_statusCodesByErrorType = map;
    }

    /// <summary>
    /// Resolves the HTTP status code registered for the given <paramref name="errorType"/>,
    /// or <see cref="Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError"/> if none was registered.
    /// Freezes the configuration: <see cref="Configure"/> throws for any call after this one.
    /// </summary>
    /// <param name="errorType">The error category to resolve.</param>
    internal static int ResolveStatusCode(ErrorTypeBase errorType)
    {
        s_locked = true;
        return s_statusCodesByErrorType.GetValueOrDefault(errorType, Status500InternalServerError);
    }

    /// <summary>
    /// Test-only hook: clears the lock and restores the default configuration. Never call this from application
    /// code — <see cref="Configure"/> is meant to run exactly once, at startup.
    /// </summary>
    internal static void ResetForTesting()
    {
        s_locked = false;
        s_statusCodesByErrorType = new ConcurrentDictionary<ErrorTypeBase, int>(DefaultStatusCodesByErrorType);
    }
}

/// <summary>
/// Builds the configuration applied by <see cref="ResultHttpOptions.Configure"/>. <see cref="CustomMap"/> is
/// applied after the built-in defaults (if <see cref="UseDefault"/>), overriding any that share the same
/// <see cref="ErrorTypeBase"/>.
/// </summary>
public sealed class ResultHttpOptionsBuilder
{
    /// <summary>Whether the built-in <see cref="ErrorType"/> mappings are seeded before <see cref="CustomMap"/> is applied. Defaults to <see langword="true"/>.</summary>
    public bool UseDefault { get; set; } = true;

    /// <summary>An entire replacement map of <see cref="ErrorTypeBase"/> to HTTP status code, applied after the built-in defaults (if any).</summary>
    public IReadOnlyDictionary<ErrorTypeBase, int>? CustomMap { get; set; }
}

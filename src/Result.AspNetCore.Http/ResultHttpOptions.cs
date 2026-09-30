using System.Collections.Concurrent;
using static Davish.Result.ErrorType;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Davish.Result;

/// <summary>
/// Configures the HTTP status code that <see cref="ResultToMinimalResultExtension"/> maps each <see cref="ErrorType"/> to.
/// This is process-wide static state, applied immediately when <see cref="Configure"/> runs rather than resolved
/// per-request from a DI container — the same idea as Dapper's <c>SqlMapper.Settings</c>: a plain, freely
/// reconfigurable global, not a "configure once and freeze" builder. Configure it via
/// <see cref="ResultAspNetCoreOptions.ConfigureStatusCodes"/>, e.g. from <c>Program.cs</c>.
/// </summary>
/// <remarks>
/// <see cref="ErrorType"/> is a <see langword="record struct"/>, so error types are matched by
/// <see cref="ErrorType.Value"/>, not by reference: two <see cref="ErrorType"/> values that share the same
/// <c>Name</c> are treated as the same category, even if constructed separately.
/// </remarks>
public static class ResultHttpOptions
{
    private static readonly IReadOnlyDictionary<ErrorType, int> DefaultStatusCodesByErrorType = new Dictionary<ErrorType, int>
    {
        [Validation] = Status400BadRequest,
        [NullValue] = Status400BadRequest,
        [NotFound] = Status404NotFound,
        [BadRequest] = Status400BadRequest,
        [Unauthorized] = Status401Unauthorized,
        [Forbidden] = Status403Forbidden,
        [Conflict] = Status409Conflict,
        [TooManyRequests] = Status429TooManyRequests,
        [Unexpected] = Status500InternalServerError,
        [ServiceUnavailable] = Status503ServiceUnavailable
    };

    private static ConcurrentDictionary<ErrorType, int> s_statusCodesByErrorType = new(DefaultStatusCodesByErrorType);

    /// <summary>
    /// Replaces the current configuration wholesale. Safe to call more than once, even after status codes have
    /// already been resolved for real requests — the new mapping takes effect immediately for anything resolved
    /// afterward, the same way reassigning a Dapper <c>SqlMapper.Settings</c> property would. Only reachable via
    /// <see cref="ResultAspNetCoreOptions.ConfigureStatusCodes"/>; there's no public entry point that bypasses it.
    /// </summary>
    /// <param name="configure">Builds the new configuration: whether to seed the built-in mappings, and any custom ones.</param>
    internal static void Configure(Action<ResultHttpOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ResultHttpOptionsBuilder();
        configure(builder);

        var map = new ConcurrentDictionary<ErrorType, int>();

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
    /// </summary>
    /// <param name="errorType">The error category to resolve.</param>
    internal static int ResolveStatusCode(ErrorType errorType) =>
        s_statusCodesByErrorType.GetValueOrDefault(errorType, Status500InternalServerError);

    /// <summary>Test-only hook: restores the default configuration.</summary>
    internal static void ResetForTesting() =>
        s_statusCodesByErrorType = new ConcurrentDictionary<ErrorType, int>(DefaultStatusCodesByErrorType);
}

/// <summary>
/// Builds the configuration applied by <see cref="ResultAspNetCoreOptions.ConfigureStatusCodes"/>. <see cref="CustomMap"/>
/// is applied after the built-in defaults (if <see cref="UseDefault"/>), overriding any that share the same
/// <see cref="ErrorType"/>.
/// </summary>
public sealed class ResultHttpOptionsBuilder
{
    /// <summary>Whether the built-in <see cref="ErrorType"/> mappings are seeded before <see cref="CustomMap"/> is applied. Defaults to <see langword="true"/>.</summary>
    public bool UseDefault { get; set; } = true;

    /// <summary>An entire replacement map of <see cref="ErrorType"/> to HTTP status code, applied after the built-in defaults (if any).</summary>
    public IReadOnlyDictionary<ErrorType, int>? CustomMap { get; set; }
}

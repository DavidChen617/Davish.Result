namespace Davish.Result;

/// <summary>
/// Converts a <see cref="Result"/> or <see cref="Result{TValue}"/> into a Minimal API result. Success is written
/// immediately; failure is deferred to the registered <see cref="IMinimalApiFailureHandler"/> at execution time
/// (see <see cref="MinimalApiResult"/>/<see cref="MinimalApiResult{TValue}"/>), which requires
/// <c>services.AddResultAspNetCore(o => o.AddMinimalApiResult(...))</c> to be registered.
/// </summary>
public static class ResultToMinimalResultExtension
{
    /// <summary>Converts a successful result to <c>200 OK</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiOkResult ToOk(this Result result) => new(result);

    /// <summary>Converts a successful result to <c>204 No Content</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiNoContentResult ToNoContent(this Result result) => new(result);

    /// <summary>Converts a successful result to <c>201 Created</c> at the given route, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedResult ToCreated(this Result result, string routeName, object? routeValues = null) =>
        new(result, routeName, routeValues);

    /// <summary>Converts a successful result to <c>202 Accepted</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAcceptedResult ToAccepted(this Result result, string? uri = null) => new(result, uri);

    /// <summary>Converts a successful result to <c>200 OK</c> with its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiOkResult<T> ToOk<T>(this Result<T> result) where T : notnull => new(result);

    /// <summary>Converts a successful result to <c>204 No Content</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiNoContentResult ToNoContent<T>(this Result<T> result) where T : notnull => new(result);

    /// <summary>Converts a successful result to <c>201 Created</c> at the given route with its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedResult<T> ToCreated<T>(this Result<T> result, string routeName, Func<T, object?>? routeValues = null)
        where T : notnull =>
        new(result, routeName, routeValues);

    /// <summary>Converts a successful result to <c>202 Accepted</c> with its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAcceptedResult<T> ToAccepted<T>(this Result<T> result, string? uri = null) where T : notnull =>
        new(result, uri);
}

---
title: ASP.NET Core setup
weight: 40
---

Namespace: `Davish.Result` (package `Davish.Result.AspNetCore.Http`, targets `net10.0` only).

## `AddResultAspNetCore`

```csharp
public static IServiceCollection AddResultAspNetCore(
    this IServiceCollection services,
    Action<ResultAspNetCoreOptions> configureOptions);
```

Registers the exception-to-`Result` mapping dispatcher and calls `services.AddProblemDetails()`.
Requires `app.UseExceptionHandler()` and `o.AddMinimalApiResult()` for exception mapping to
actually take effect.

## `ResultAspNetCoreOptions`

Passed into `AddResultAspNetCore`'s configuration delegate.

```csharp
public sealed class ResultAspNetCoreOptions
{
    public void MapExceptionToResult<TException>(Func<HttpContext, TException, Result> mapper)
        where TException : Exception;

    public void MapExceptionToResult<TException>(
        Func<HttpContext, TException, CancellationToken, ValueTask<Result>> mapper)
        where TException : Exception;

    public void MapExceptionToResult<TException, TMapper>()
        where TException : Exception
        where TMapper : class, IExceptionMapper<TException>;

    public void AddMinimalApiResult(Action<MinimalApiResultOptions>? configureOptions = null);

    public void ConfigureStatusCodes(Action<ResultHttpOptionsBuilder> configure);

    public void ConfigureProblemDetails(Action<ProblemDetailsOptions> configure);
}
```

- `MapExceptionToResult<TException>(...)` — two overloads, sync and async, register a
  translation for `TException` (and its subclasses; most-specific match wins when multiple are
  registered along the same inheritance chain).
- `MapExceptionToResult<TException, TMapper>()` — DI-resolved variant. `TMapper` is registered
  `TryAddTransient` and resolved per exception from `HttpContext.RequestServices`, not captured
  at registration time (avoids a captive dependency if `TMapper` depends on scoped services).
- `AddMinimalApiResult(configureOptions)` — registers the default `IMinimalApiFailureHandler`
  (`TryAddSingleton`, so a prior registration wins) and the exception handler that dispatches
  `MapExceptionToResult` registrations; `configureOptions` is optional.
- `ConfigureStatusCodes(...)` — replaces the process-wide status code mapping (see
  [Status code mapping](status-code-mapping.md)). Applied immediately and not locked, so it can
  be called again (for example once per test host).
- `ConfigureProblemDetails(...)` — configures the `ProblemDetailsOptions` used by the
  `services.AddProblemDetails()` call `AddResultAspNetCore` already makes. Host-agnostic — the
  same options MVC's `[ApiController]`/`ControllerBase.Problem()` use.

## `MinimalApiResultOptions`

Passed into `AddMinimalApiResult`'s optional configuration delegate.

```csharp
public sealed class MinimalApiResultOptions
{
    public void MinimalApiFailureHandler<THandler>() where THandler : class, IMinimalApiFailureHandler;
}
```

Replaces the registered `IMinimalApiFailureHandler` singleton (`services.Replace(...)`) with
`THandler`. See [Customizing failure handling](../guides/customizing-failure-handling.md).

## `IExceptionMapper<TSource>`

```csharp
public interface IExceptionMapper<in TSource> where TSource : Exception
{
    ValueTask<Result> MapAsync(HttpContext context, TSource source, CancellationToken cancellationToken);
}
```

Implement this for a `MapExceptionToResult<TException, TMapper>()` registration that needs
services resolved from DI.

## Example

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.MapExceptionToResult<BookingLockedException>((context, exception) =>
        Result.Failure(new Error("Booking.Locked", "This booking is already confirmed.", ErrorType.Conflict)));

    o.AddMinimalApiResult();
});

var app = builder.Build();

app.UseExceptionHandler(); // required for MapExceptionToResult to take effect
```

## Related

- [Setting up the ASP.NET Core integration](../guides/setting-up-aspnetcore-integration.md)
- [Mapping exceptions to Result](../guides/mapping-exceptions-to-results.md)
- [ASP.NET Core integration model](../concepts/aspnetcore-integration-model.md)

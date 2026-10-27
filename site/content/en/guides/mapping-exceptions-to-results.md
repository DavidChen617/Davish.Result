---
title: Mapping exceptions to Result
weight: 80
---

For code that throws instead of returning a `Result` (a third-party library, for example),
`MapExceptionToResult` converts a caught exception into the same failure pipeline as `ToOk()`.

## Register a mapping with a delegate

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.MapExceptionToResult<BookingLockedException>((context, exception) =>
        Result.Failure(new Error("Booking.Locked", "This booking is already confirmed.", ErrorType.Conflict)));

    o.AddMinimalApiResult();
});
```

A synchronous delegate (`Func<HttpContext, TException, Result>`) and an asynchronous one
(`Func<HttpContext, TException, CancellationToken, ValueTask<Result>>`) are both available —
pick whichever overload matches whether the mapping needs to do async work (e.g. read
something from the database).

`app.UseExceptionHandler()` is required for this to take effect — `AddResultAspNetCore` only
registers the DI services, it doesn't wire up the middleware pipeline.

## Matching is by inheritance, most-specific wins

Registering a mapper for a base exception type also matches its subclasses. If you register
mappers for both a base and a derived exception type, the most-specific one wins for instances
of the derived type.

## Register a mapping that needs dependency injection

`MapExceptionToResult<TException, TMapper>()` — where `TMapper : IExceptionMapper<TException>`
— is available for mappers that need services resolved from DI:

```csharp
public sealed class BookingLockedExceptionMapper : IExceptionMapper<BookingLockedException>
{
    public ValueTask<Result> MapAsync(HttpContext context, BookingLockedException source, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Result.Failure(new Error("Booking.Locked", "This booking is already confirmed.", ErrorType.Conflict)));
}

builder.Services.AddResultAspNetCore(o =>
{
    o.MapExceptionToResult<BookingLockedException, BookingLockedExceptionMapper>();
});
```

`TMapper` is registered as transient and resolved per exception from the current request's
`HttpContext.RequestServices` — not captured once at registration time — so it can safely
depend on scoped services without becoming a captive dependency.

## Related

- [ASP.NET Core integration model](../concepts/aspnetcore-integration-model.md) — how exception
  mapping and `Result` → `IResult` conversion converge on the same failure handler.
- [Minimal API results](../reference/minimal-api-results.md) — `IExceptionMapper<TSource>`.

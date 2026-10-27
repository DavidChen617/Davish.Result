---
title: ASP.NET Core integration model
weight: 30
---

`Davish.Result.AspNetCore.Http` (targets `net10.0` only) bridges `Result`/`Result<T>` to
Minimal API's `IResult`, and lets exceptions your own code doesn't catch flow into the same
failure pipeline.

## Two directions of translation

The design (see the package's `IFailureHandler<TResult>`/`IExceptionMapper<TSource>` split)
keeps two translation directions independent, joining only at the failure handler:

- **`Result` → `IResult`** (Minimal-API-specific): `ToOk()`/`ToCreated()`/`ToAccepted()`/`ToNoContent()`
  and the other `ToXxx()` shapes (text, JSON, files, redirects, authentication, ...) convert
  success immediately. Failure is deferred to whatever `IMinimalApiFailureHandler` is
  registered.
- **`Exception` → `Result`** (host-agnostic): `MapExceptionToResult<TException>(...)` registers
  a translation that runs when `TException` (or a subclass) escapes uncaught, converting it into
  a `Result` that then flows through the *same* failure handler as a directly-returned `Result.Failure(...)`.

## Success is written immediately; failure is deferred

`MinimalApiResult`/`MinimalApiResult<TValue>` (the base classes behind `ToOk()` and friends)
implement `IResult.ExecuteAsync` as:

```csharp
public async Task ExecuteAsync(HttpContext httpContext)
{
    var httpResult = _result.IsSuccess
        ? CreateSuccessResult()
        : await ResolveFailureAsync(httpContext);   // resolves IMinimalApiFailureHandler from DI, at execution time

    await httpResult.ExecuteAsync(httpContext);
}
```

Because failure resolution happens through `httpContext.RequestServices` at execution time
(not when `ToOk()` is called), swapping the registered `IMinimalApiFailureHandler` changes every
endpoint's failure response without touching endpoint code.

## Which types are open for extension is deliberate

`MinimalApiResult`/`MinimalApiResult<TValue>` are `public abstract` specifically so you can add
your own success shape (e.g. a `206 Partial Content` result) by inheriting from them and
reusing their failure-handling logic. The built-in shapes (`MinimalApiOkResult`,
`MinimalApiCreatedResult`, `MinimalApiCreatedAtLocationResult`, `MinimalApiAcceptedResult`,
`MinimalApiAcceptedAtRouteResult`, `MinimalApiNoContentResult`, `MinimalApiContentResult`,
`MinimalApiJsonResult`, `MinimalApiFileResult`, `MinimalApiRedirectResult`,
`MinimalApiAuthenticationResult`, `MinimalApiStatusCodeResult`, the server-sent-events shapes, and
the generic `<T>` variants of those that carry a value) are `public sealed` — they're the default
implementations, not further extension points. `IFailureHandler<TResult>` is generic
specifically so a future host-specific failure handler (e.g. for MVC's `IActionResult`) could
reuse the same abstraction; today only `IMinimalApiFailureHandler = IFailureHandler<IResult>`
exists.

## `ToOk()` etc. return concrete types, not `IResult`

`ToOk()` returns `MinimalApiOkResult`/`MinimalApiOkResult<T>`, not `IResult`. This is required
for ASP.NET Core's OpenAPI metadata: `IEndpointMetadataProvider.PopulateMetadata` is invoked
based on the endpoint delegate's *declared* return type, and only types that implement
`IEndpointMetadataProvider` get their success-response metadata registered. Declaring the
return type as bare `IResult` would silently drop that metadata. Since every concrete type
still implements `IResult`, this isn't a source-breaking change for callers who just `return
result.ToOk();`.

## Exception mapping matches by inheritance, most-specific wins

`MapExceptionToResult<TException>(...)` registrations are matched against a thrown exception's
runtime type by walking up its base-type chain (the same way a `catch` clause matches),
starting from the most-derived type. If mappers are registered for both a base and a derived
exception type, the derived-type mapper wins for instances of the derived type. Resolution is
cached per concrete exception type.

## `app.UseExceptionHandler()` is required, and easy to forget

`AddResultAspNetCore` only registers DI services — it does not wire up the ASP.NET Core
exception-handling middleware. `MapExceptionToResult` registrations only take effect once
`app.UseExceptionHandler()` is added to the pipeline. `AddResultAspNetCore` also calls
`services.AddProblemDetails()` internally, which is the fallback ASP.NET Core's parameterless
`app.UseExceptionHandler()` needs at startup for exceptions that no registered exception mapper
handles — without it, the app throws at startup rather than at request time.

See [ASP.NET Core setup](../guides/setting-up-aspnetcore-integration.md),
[Converting results to HTTP responses](../guides/converting-results-to-http-responses.md), and
[Mapping exceptions to Result](../guides/mapping-exceptions-to-results.md) for task-oriented
walkthroughs.

---
title: Setting up the ASP.NET Core integration
weight: 50
---

Requires `Davish.Result.AspNetCore.Http` (targets `net10.0` only):

```bash
dotnet add package Davish.Result.AspNetCore.Http
```

## Register and wire up the middleware

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.AddMinimalApiResult();   // registers ToOk()/ToCreated()/... failure handling — skip only if you never use them
});

var app = builder.Build();

app.UseExceptionHandler();   // required if you use MapExceptionToResult (see below)
```

`AddResultAspNetCore` registers the exception-to-`Result` mapping dispatcher and calls
`services.AddProblemDetails()` internally (the fallback ASP.NET Core's parameterless
`app.UseExceptionHandler()` needs at startup). `o.AddMinimalApiResult()` additionally registers
the default `IMinimalApiFailureHandler` and the exception handler that lets
`MapExceptionToResult` registrations actually take effect.

`app.UseExceptionHandler()` is required for `MapExceptionToResult` to take effect —
`AddResultAspNetCore` only registers the DI services, it doesn't wire up the middleware
pipeline itself.

## What each piece is for

| Call | Registers |
|---|---|
| `AddResultAspNetCore(o => ...)` | Exception-mapper dispatcher, `services.AddProblemDetails()`. |
| `o.AddMinimalApiResult(...)` | Default `IMinimalApiFailureHandler`, the exception handler behind `MapExceptionToResult`. |
| `o.MapExceptionToResult<TException>(...)` | One exception-to-`Result` translation (see [Mapping exceptions to Result](mapping-exceptions-to-results.md)). |
| `o.ConfigureStatusCodes(...)` | The `ErrorType` → HTTP status code table (see [Configuring status codes](configuring-status-codes.md)). |
| `o.ConfigureProblemDetails(...)` | `ProblemDetailsOptions` used by `AddProblemDetails()`. |

## Where to go next

- [Converting results to HTTP responses](converting-results-to-http-responses.md) — `ToOk()`,
  `ToCreated()`, `ToAccepted()`, `ToNoContent()`.
- [Customizing failure handling](customizing-failure-handling.md) — replace the default
  `ProblemDetails` failure shape.
- [Mapping exceptions to Result](mapping-exceptions-to-results.md) — convert exceptions from
  code that throws instead of returning a `Result`.
- [Configuring status codes](configuring-status-codes.md) — register your own `ErrorType` →
  status code entries.

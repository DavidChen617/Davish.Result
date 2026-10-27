---
title: Customizing failure handling
weight: 70
---

The default `IMinimalApiFailureHandler` produces a `ProblemDetails` (or validation problem, if
`Error.Fields` is populated) using the [status code mapping](configuring-status-codes.md).

## Replace the default handler

```csharp
public sealed class MyFailureHandler : IMinimalApiFailureHandler
{
    public ValueTask<IResult> HandleAsync(Result result, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Results.Json(new { error = result.Error.Code }));
}

builder.Services.AddResultAspNetCore(o =>
{
    o.AddMinimalApiResult(m => m.MinimalApiFailureHandler<MyFailureHandler>());
});
```

`HandleAsync` is only ever called for a failed `Result` (`Result.IsSuccess` is `false`). It
replaces the singleton registration for `IMinimalApiFailureHandler` — the same handler then
backs both `ToOk()`/`ToCreated()`/etc. failures and exceptions routed through
`MapExceptionToResult`, since both paths converge on the same registered handler.

## Customizing ProblemDetails instead of replacing the handler

If you want to keep the default `ProblemDetails`/`ValidationProblemDetails` shape but add or
adjust fields, configure `ProblemDetailsOptions` instead of writing a custom handler.
`o.ConfigureProblemDetails(...)` (from the same `AddResultAspNetCore` call) configures the same
`ProblemDetailsOptions` ASP.NET Core's `[ApiController]`/`ControllerBase.Problem()` use — it's
not specific to Minimal APIs:

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.ConfigureProblemDetails(p => p.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
});
```

## Related

- [ASP.NET Core integration model](../concepts/aspnetcore-integration-model.md) — how failure
  resolution fits into `MinimalApiResult.ExecuteAsync`.
- [Minimal API results](../reference/minimal-api-results.md) — `IFailureHandler<TResult>` /
  `IMinimalApiFailureHandler`.

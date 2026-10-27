---
title: Status code mapping
weight: 60
---

Namespace: `Davish.Result` (package `Davish.Result.AspNetCore.Http`).

## `ResultHttpOptions`

```csharp
public static class ResultHttpOptions
{
    // internal: static void Configure(Action<ResultHttpOptionsBuilder> configure)
    // internal: static int ResolveStatusCode(ErrorType errorType)
}
```

`ResultHttpOptions` is a public static class, but its members are `internal` — configure the
mapping through `ResultAspNetCoreOptions.ConfigureStatusCodes(...)` (see
[ASP.NET Core setup](aspnetcore-setup.md)).

- `ConfigureStatusCodes` replaces the mapping wholesale, applied immediately. There is no
  lock: calling it again — even after status codes have already been resolved — simply
  replaces the mapping.
- Built-in defaults (seeded when `ResultHttpOptionsBuilder.UseDefault` is `true`, the default):

  | `ErrorType` | Status code |
  |---|---|
  | `Validation` | 400 |
  | `NullValue` | 400 |
  | `BadRequest` | 400 |
  | `NotFound` | 404 |
  | `Unauthorized` | 401 |
  | `Forbidden` | 403 |
  | `Conflict` | 409 |
  | `TooManyRequests` | 429 |
  | `Unexpected` | 500 |
  | `ServiceUnavailable` | 503 |

- Status codes are resolved internally by `ErrorType.ToStatusCode()` (below); an unregistered
  `ErrorType` resolves to `500`.
- This is process-wide static state (a `ConcurrentDictionary<ErrorType, int>` swapped
  atomically on each configure), not resolved per-request from DI — see
  [Error-to-status-code mapping](../concepts/status-code-mapping.md).

## `ResultHttpOptionsBuilder`

```csharp
public sealed class ResultHttpOptionsBuilder
{
    public bool UseDefault { get; set; } = true;
    public IReadOnlyDictionary<ErrorType, int>? CustomMap { get; set; }
}
```

`CustomMap` is applied after the built-in defaults (if `UseDefault`), overriding any entries
that share the same `ErrorType`.

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.ConfigureStatusCodes(v =>
    {
        v.CustomMap = new Dictionary<ErrorType, int>
        {
            [OrderErrorType.OutOfStock] = StatusCodes.Status409Conflict,
        };
    });
});
```

## `ErrorTypeHttpExtension`

```csharp
public static class ErrorTypeHttpExtension
{
    public static int ToStatusCode(this ErrorType errorType);
}
```

Resolves the HTTP status code configured for `errorType` via `ConfigureStatusCodes`
(500 if none was registered). Exposed publicly so consumers writing their own `ToXxx()` Minimal
API result extensions can reuse the configured mapping.

## Related

- [Configuring status codes](../guides/configuring-status-codes.md)
- [Error-to-status-code mapping](../concepts/status-code-mapping.md)

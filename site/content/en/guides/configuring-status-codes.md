---
title: Configuring status codes
weight: 90
---

Failures map to a status code by `Error.Type`. Built-in categories map as you'd expect
(`Validation`/`NullValue`/`BadRequest` → 400, `NotFound` → 404, `Unauthorized` → 401,
`Forbidden` → 403, `Conflict` → 409, `TooManyRequests` → 429, `ServiceUnavailable` → 503, `Unexpected` and anything
unregistered → 500).

## Register your own categories

Configure the mapping from the same `AddResultAspNetCore` call as everything else, via
`o.ConfigureStatusCodes(...)`:

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.ConfigureStatusCodes(v =>
    {
        // v.UseDefault = false;   // opt out of the built-in mappings above

        v.CustomMap = new Dictionary<ErrorType, int>
        {
            [OrderErrorType.OutOfStock] = StatusCodes.Status409Conflict,
            [OrderErrorType.PaymentDeclined] = StatusCodes.Status402PaymentRequired,
        };
    });
});
```

`ResultHttpOptions.Configure` is `internal`, so `ConfigureStatusCodes` is the only entry point.
This is process-wide static configuration, applied immediately rather than resolved from a DI
container — the same idea as Dapper's
`SqlMapper.Settings`. Since `ErrorType` uses value equality, `OrderErrorType.OutOfStock` and any
other `ErrorType` you build with the same name map to the same entry.

## Calling it again replaces the mapping

There is no lock. Calling `ConfigureStatusCodes` again — even after status codes have been
resolved for real requests — replaces the mapping wholesale. This is convenient for tests that
start several hosts in one process (each host's `AddResultAspNetCore` just reapplies its
mapping). The flip side: if two hosts in the same process want *different* mappings at the same
time, whichever configured last wins for both.

## Related

- [Error-to-status-code mapping](../concepts/status-code-mapping.md) — why this is static,
  process-wide configuration rather than a DI-resolved option.
- [Status code mapping](../reference/status-code-mapping.md) — `ResultHttpOptions`,
  `ResultHttpOptionsBuilder`, `ErrorTypeHttpExtension.ToStatusCode()`.

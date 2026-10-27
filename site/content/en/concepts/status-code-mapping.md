---
title: Error-to-status-code mapping
weight: 40
---

`ResultHttpOptions` maps each `ErrorType` to an HTTP status code. `DefaultMinimalApiFailureHandler`
and `MinimalApiResult.StatusCode`/`MinimalApiResult<TValue>.StatusCode` both resolve a failed
result's status code through `ErrorType.ToStatusCode()`, which forwards to this mapping.

## Built-in defaults

| `ErrorType` | Status code |
|---|---|
| `Validation` | 400 Bad Request |
| `NullValue` | 400 Bad Request |
| `BadRequest` | 400 Bad Request |
| `NotFound` | 404 Not Found |
| `Unauthorized` | 401 Unauthorized |
| `Forbidden` | 403 Forbidden |
| `Conflict` | 409 Conflict |
| `TooManyRequests` | 429 Too Many Requests |
| `ServiceUnavailable` | 503 Service Unavailable |
| `Unexpected` | 500 Internal Server Error |
| Anything unregistered | 500 Internal Server Error |

## Process-wide static configuration, not DI

`ConfigureStatusCodes(...)` replaces the mapping wholesale, immediately, as a static operation —
it is not resolved per-request from a DI container. The doc comments compare this model
explicitly to Dapper's `SqlMapper.Settings`: a single,
process-wide table.

Because `ErrorType` is a `record struct` compared by its `Value` name, an `ErrorType` you build
yourself with the same name as `OrderErrorType.OutOfStock` maps to the same table entry as the
original — identity doesn't matter, only the name does.

## Reconfiguring is allowed

`ResultHttpOptions.Configure` is `internal`, and each `ConfigureStatusCodes` call swaps in a
fresh table; there is no lock. This matters for tests that create a new host per test in one process — each host's
`AddResultAspNetCore` just reapplies its mapping. The trade-off is the usual one for any
process-wide static: two hosts that want different mappings at the same time will overwrite each
other, so keep the mapping identical across hosts in the same process.

## `ConfigureStatusCodes` is the only public entry point

`ResultAspNetCoreOptions.ConfigureStatusCodes(Action<ResultHttpOptionsBuilder>)` lets you set the
mapping from the same `AddResultAspNetCore(o => ...)` call as everything else.

See [Configuring status codes](../guides/configuring-status-codes.md) for how to register a
custom `ErrorType` mapping, and [Status code mapping](../reference/status-code-mapping.md) for
the full API.

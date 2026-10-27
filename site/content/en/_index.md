---
title: Davish.Result
---

Davish.Result is a Result-pattern library for .NET. It models the outcome of an operation —
success or failure — as a value (`Result` / `Result<TValue>`) instead of relying on
exceptions for expected error flows. A second package converts a `Result` straight into an
ASP.NET Core Minimal API response.

## Install

```bash
dotnet add package Davish.Result                    # Result / Result<T> / Error / ErrorType
dotnet add package Davish.Result.AspNetCore.Http     # Result -> Minimal API IResult
```

Everything lives under a single root namespace:

```csharp
using Davish.Result;
```

## Where to start

- **[Versions](versions.md)** — current package versions and upgrade notes.
- **[Getting started](getting-started.md)** — install the core package and produce/read your
  first `Result`.
- **Concepts** — how the library is put together and why:
  - [The Result and Error model](concepts/result-and-error-model.md) — the success/failure
    invariant, structured errors, cause chains, and exception bridging.
  - [ASP.NET Core integration model](concepts/aspnetcore-integration-model.md) — how a
    `Result` becomes an `IResult`, and where failure handling and exception mapping fit in.
  - [Error-to-status-code mapping](concepts/status-code-mapping.md) — why the mapping is
    process-wide static configuration, and how reconfiguring it behaves.
- **Guides** — task-oriented how-tos:
  - [Creating results](guides/creating-results.md)
  - [Building structured errors](guides/building-structured-errors.md)
  - [Wrapping caught exceptions](guides/wrapping-exceptions.md)
  - [Setting up the ASP.NET Core integration](guides/setting-up-aspnetcore-integration.md)
  - [Converting results to HTTP responses](guides/converting-results-to-http-responses.md)
  - [Customizing failure handling](guides/customizing-failure-handling.md)
  - [Mapping exceptions to Result](guides/mapping-exceptions-to-results.md)
  - [Configuring status codes](guides/configuring-status-codes.md)
- **Reference**:
  - [Result and Result\<TValue\>](reference/result.md)
  - [Error, ErrorType, and ExceptionalError](reference/error.md)
  - [ASP.NET Core setup](reference/aspnetcore-setup.md)
  - [Minimal API results](reference/minimal-api-results.md)
  - [Status code mapping](reference/status-code-mapping.md)
  - [Exceptions](reference/exceptions.md)

## Packages at a glance

| Package | Purpose | Targets |
|---|---|---|
| `Davish.Result` | Core `Result`/`Result<TValue>`, `Error`/`ErrorType`, exception bridging. | `netstandard2.0`, `net10.0` |
| `Davish.Result.AspNetCore.Http` | Converts `Result`/`Result<T>` into a Minimal API `IResult`, pluggable failure handling, exception-to-`Result` mapping, error-type-to-status-code configuration. | `net10.0` |

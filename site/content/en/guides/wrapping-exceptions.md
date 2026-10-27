---
title: Wrapping caught exceptions
weight: 40
---

`ExceptionalError` wraps a caught exception as an `Error`, mapping its `InnerException` chain
onto `Error.Causes`.

## `Result.Failure(exception)` — quick, but can leak details

```csharp
try
{
    return Result.Success(File.ReadAllText(path));
}
catch (Exception ex)
{
    return Result.Failure<string>(ex);   // Error.Description is the raw ex.Message
}
```

`Result.Failure(exception)` sets `Error.Description` to the raw `Exception.Message`, which can
contain implementation details (SQL fragments, file paths, internal type names). Avoid this
overload if the result could reach an HTTP response and the exception message isn't something
you want an untrusted client to see.

## `Result.Failure(exception, error)` — client-safe

```csharp
catch (Exception ex)
{
    return Result.Failure<Order>(ex, new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected));
}

// result.Error.Description               -> "Could not load the order."  (safe to expose)
// result.Error.InnerError.Description     -> the raw ex.Message           (not exposed unless you read it)
```

This surfaces the `Error` you provide, with the exception attached only as its `InnerError` —
so the raw exception message never appears in the primary `Error.Description`.

## Getting the original exception back for logging

Pattern-match on `ExceptionalError` to recover the original `Exception` instance — `result.Error`
itself when using `Result.Failure(exception)`, or `result.Error.InnerError` when using
`Result.Failure(exception, error)`:

```csharp
if (result.Error.InnerError is ExceptionalError { Exception: var ex })
    logger.LogError(ex, "Operation failed");   // full stack trace, exception type, etc.
```

## Related

- [The Result and Error model](../concepts/result-and-error-model.md) — how `ExceptionalError`
  fits into the cause-chain model.
- [Error, ErrorType, and ExceptionalError](../reference/error.md) — `ExceptionalError.From`
  overloads.

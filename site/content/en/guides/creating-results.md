---
title: Creating results
weight: 10
---

## Non-generic `Result` (no value)

```csharp
Result ok = Result.Success();
Result bad = Result.Failure(new Error("User.Locked", "The account is locked"));
```

## `Result<TValue>` (carries a value on success)

```csharp
Result<int> value = Result.Success(42);
Result<int> failed = Result.Failure<int>(new Error("Parse.Failed", "Not a number"));
```

`Result.Failure(Error.None)` and `Result.Success<TValue>` with a `null` value both throw
`InvalidResultStateException` — a success/failure combination must be internally consistent.
See [The Result and Error model](../concepts/result-and-error-model.md).

## Reading a result

```csharp
if (result.IsSuccess)
    Use(result.Value);           // Result<T>.Value throws ResultValueUnavailableException on failure
else
    Log(result.Error.Description);
```

`ResultValueUnavailableException` and `InvalidResultStateException` both derive from
`ResultException`, so `catch (ResultException)` handles either.

Both `Result` and `Result<TValue>` support deconstruction:

```csharp
var (isSuccess, error) = result;                  // Result
var (isSuccess, value, error) = typedResult;       // Result<TValue> — value is default on failure, never throws
```

## Implicit conversions from a value or an `Error`

A value or an `Error` converts to a result implicitly, so a factory method can just `return`:

```csharp
Result<string> GetName(int id)
{
    if (id <= 0)
        return new Error("Id.Invalid", "Id must be positive");   // Error  -> failed result

    return "David";                                              // value  -> successful result
}
```

A `null` value converts to a failure carrying `Error.NullValue` rather than a successful result
holding `null` — `Result<TValue>` requires `TValue : notnull`.

## Related

- [Building structured errors](building-structured-errors.md) — field-level messages and cause
  chains.
- [Wrapping caught exceptions](wrapping-exceptions.md) — `Result.Failure(exception)` and
  `Result.Failure(exception, error)`.
- [Result and Result\<TValue\>](../reference/result.md) — full factory method list.

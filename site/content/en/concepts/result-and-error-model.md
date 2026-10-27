---
title: The Result and Error model
weight: 10
---

## Success and failure are mutually exclusive by construction

`Result.IsSuccess` is computed, not stored: `Error == Error.None`. A `Result` can only be
built through `Result.Success()`/`Result.Success<TValue>(value)` or
`Result.Failure(error)`/`Result.Failure<TValue>(error)`, and both constructors enforce the
invariant:

- `Result.Failure(error)` throws `InvalidResultStateException` if `error == Error.None`
  (`Error.None` describes a success, so it can't also describe a failure).
- `Result<TValue>`'s constructor throws `InvalidResultStateException` if `error == Error.None`
  and `value` is `null` (a success must carry a value).

`Result` has no public constructor (only a `protected` one for derived types), so outside code
creates results through the static factory methods, or through the implicit conversions that
call them.

## `Result<TValue>` requires `TValue : notnull`

Both `Result.Success<TValue>` and `Result.Failure<TValue>` constrain `TValue : notnull`. A
`null` value doesn't produce a successful `Result<TValue>` holding `null` — the implicit
conversion from a value (`public static implicit operator Result<TValue>(TValue? value)`)
turns a `null` into a failure carrying `Error.NullValue` instead:

```csharp
string? value = null;
Result<string> result = value;   // failed result, result.Error == Error.NullValue
```

## Errors are immutable and derive new instances

`Error` is a `record` with an `AddFieldError`/`WithMetadata`/`CausedBy` API that never mutates the receiver —
each call returns a new `Error` with the addition applied. This matters for the two canonical
singletons, `Error.None` and `Error.NullValue`: calling `Error.NullValue.AddFieldError(...)`
does not pollute the shared singleton; you must use the returned value.

`Error` equality is structural (record equality), comparing `Code`, `Description`, `Type`,
`Fields`, `Metadata`, and `Causes` (order-sensitive for `Causes` and `Fields`' message lists). Two
independently constructed `Error` instances with the same values are equal.

## `ErrorType` is a value, not an enum

`ErrorType` is a `readonly record struct` wrapping a `string Value`. Built-in categories
(`ErrorType.Validation`, `ErrorType.NotFound`, etc.) are `static readonly` fields — declaring
your own category is just declaring another `static readonly ErrorType` with a different name.
Because it's a record struct, two `ErrorType` values with the same name are equal regardless of
which field (or `new ErrorType(...)` call) produced them:

```csharp
var notFoundByValue = new ErrorType(ErrorType.NotFound.Value);
notFoundByValue == ErrorType.NotFound;   // true
```

Constructing an `ErrorType` with a `null` or empty name throws `InvalidErrorTypeException`.

## Cause chains mirror `Exception.InnerException`

`Error.Causes` is an ordered, immutable list; `Error.InnerError` is `Causes[0]` (or `null` if
empty) — the same relationship `Exception.InnerException` has to
`AggregateException.InnerExceptions`. `GetRootCause()` walks down the chain of causes as long
as each node has exactly one cause, and stops at a node with zero causes (a leaf) or more than
one (an aggregation point, mirroring `AggregateException.GetBaseException`).

## `ExceptionalError` bridges caught exceptions into the model

`ExceptionalError : Error` wraps a caught `Exception`: `Code` is the exception's type name,
`Description` is `Exception.Message`, and `Type` is `ErrorType.Unexpected`. `Exception.InnerException`
is recursively wrapped and attached via `CausedBy`, so `Error.GetRootCause()` on the resulting
error walks the same chain `Exception.InnerException` would.

`Result.Failure(exception)` uses `ExceptionalError.From(exception)` directly — the raw
`Exception.Message` becomes `Error.Description`. `Result.Failure(exception, error)` instead
uses `ExceptionalError.From(exception, error)`, which wraps the exception as the `InnerError` of
the client-safe `error` you provide, keeping the raw exception message out of `Description`.
See [Wrapping caught exceptions](../guides/wrapping-exceptions.md) for when to use which.

## Exceptions this package itself throws

All library-thrown exceptions derive from the abstract `ResultException : InvalidOperationException`,
so `catch (ResultException)` handles any of them:

- `InvalidResultStateException` — an inconsistent success/error combination was constructed.
- `ResultValueUnavailableException` — `Result<TValue>.Value` was read on a failed result.
- `InvalidErrorTypeException` — an `ErrorType` was constructed with a null/empty name.

See [Exceptions](../reference/exceptions.md) for the full list.

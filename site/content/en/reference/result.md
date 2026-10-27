---
title: Result and Result<TValue>
weight: 10
---

Namespace: `Davish.Result` (package `Davish.Result`).

## `Result`

Represents the outcome of an operation as either a success or a failure carrying an `Error`.

```csharp
public class Result
{
    public bool IsSuccess { get; }     // Error == Error.None
    public Error Error { get; }

    protected Result(Error error);

    public void Deconstruct(out bool isSuccess, out Error error);

    public static Result Success();
    public static Result Failure(Error error);
    public static Result Failure(Exception exception);
    public static Result Failure(Exception exception, Error error);

    public static Result<TValue> Success<TValue>(TValue value) where TValue : notnull;
    public static Result<TValue> Failure<TValue>(Error error) where TValue : notnull;
    public static Result<TValue> Failure<TValue>(Exception exception) where TValue : notnull;
    public static Result<TValue> Failure<TValue>(Exception exception, Error error) where TValue : notnull;
}
```

- `IsSuccess` is computed from `Error == Error.None`; there is no separately stored flag.
- The constructor is `protected` — build instances through the static factories, or by
  deriving your own result type on top of `Result`.
- `Failure(error)` throws `InvalidResultStateException` if `error == Error.None`.
- `Failure(exception)` builds the error via `ExceptionalError.From(exception)` — see
  [Error, ErrorType, and ExceptionalError](error.md) for the safety note about
  `Error.Description` exposing the raw exception message.
- `Failure(exception, error)` builds the error via `ExceptionalError.From(exception, error)` —
  the exception becomes `error`'s `InnerError` instead of replacing its description.

```csharp
Result ok = Result.Success();
Result bad = Result.Failure(new Error("User.Locked", "The account is locked"));
```

## `Result<TValue>`

Represents the outcome of an operation that produces a value of type `TValue`. Inherits
`Result`.

```csharp
public class Result<TValue> : Result where TValue : notnull
{
    public TValue Value { get; }   // throws ResultValueUnavailableException if not successful

    protected internal Result(TValue? value, Error error);

    public void Deconstruct(out bool isSuccess, out TValue? value, out Error error);

    public static implicit operator Result<TValue>(TValue? value);   // null -> failure with Error.NullValue
    public static implicit operator Result<TValue>(Error error);
}
```

- `Value` throws `ResultValueUnavailableException` (carrying `Error`) when the result is not
  successful. Check `IsSuccess` first, or use deconstruction, which never throws.
- The constructor throws `InvalidResultStateException` if `error == Error.None` and `value` is
  `null` — a success must carry a non-null value.
- Deconstruction reads the underlying field directly (not `Value`), so `value` is `default`
  on failure rather than throwing.
- The implicit `TValue?` conversion is why factory methods can `return value;` directly — a
  non-null value becomes `Success(value)`, `null` becomes `Failure<TValue>(Error.NullValue)`.

```csharp
Result<int> value = Result.Success(42);
Result<int> failed = Result.Failure<int>(new Error("Parse.Failed", "Not a number"));

Result<string> GetName(int id) => id <= 0
    ? new Error("Id.Invalid", "Id must be positive")   // Error -> failed result
    : "David";                                          // value -> successful result
```

## Related

- [Creating results](../guides/creating-results.md)
- [The Result and Error model](../concepts/result-and-error-model.md)

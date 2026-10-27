---
title: Exceptions
weight: 70
---

All exceptions thrown directly by this library derive from the abstract
`ResultException : InvalidOperationException` (namespace `Davish.Result`), so
`catch (ResultException)` handles any of them.

## From `Davish.Result`

| Exception | Thrown when |
|---|---|
| `InvalidResultStateException` | `Result.Failure(error)`/`Result.Failure<TValue>(error)` is called with `error == Error.None`, or `Result<TValue>` is constructed with `error == Error.None` and a `null` value. Carries `IsSuccess` (the attempted state) and `Error`. |
| `ResultValueUnavailableException` | `Result<TValue>.Value` is read on a failed result. Carries `Error`. |
| `InvalidErrorTypeException` | `new ErrorType(value)` is called with a `null` or empty `value`. |

```csharp
public abstract class ResultException : InvalidOperationException
{
    protected ResultException(string message);
}

public sealed class InvalidResultStateException : ResultException
{
    public bool IsSuccess { get; }
    public Error Error { get; }
}

public sealed class ResultValueUnavailableException : ResultException
{
    public Error Error { get; }
}

public sealed class InvalidErrorTypeException : ResultException
{
}
```

## Related

- [The Result and Error model](../concepts/result-and-error-model.md)
- [Creating results](../guides/creating-results.md)

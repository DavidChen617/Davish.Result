---
title: Error, ErrorType, and ExceptionalError
weight: 20
---

Namespace: `Davish.Result` (package `Davish.Result`).

## `Error`

An immutable `record` describing what went wrong.

```csharp
public record Error
{
    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }                                              // defaults to ErrorType.Validation
    public IReadOnlyList<Error> Causes { get; }                                 // defaults to []
    public Error? InnerError { get; }                                           // Causes[0], or null
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Fields { get; }   // defaults to {}
    public IReadOnlyDictionary<string, object?> Metadata { get; }               // defaults to {}

    public Error(string code, string description);
    public Error(string code, string description, ErrorType type);
    public Error(string code, string description, Dictionary<string, List<string>> fields);   // throws ArgumentNullException if fields is null

    public static Error None { get; }        // "" / "" / ErrorType.None — used by successful results
    public static Error NullValue { get; }   // "Null.Value" / "Null value was provided" / ErrorType.NullValue

    public static implicit operator Result(Error error);

    public Error AddFieldError(string field, string message);
    public Error AddFieldError(string field, ICollection<string> messages);

    public Error WithMetadata(string key, object? value);
    public Error WithMetadata(IReadOnlyDictionary<string, object?> metadata);   // throws ArgumentNullException if null

    public Error CausedBy(Error cause);
    public Error CausedBy(IReadOnlyList<Error> causes);

    public Error GetRootCause();
}
```

- `Type` defaults to `ErrorType.Validation` when not specified in the constructor.
- `AddFieldError`/`WithMetadata`/`CausedBy` never mutate the receiver — each returns a new `Error`. Calling
  any of them on a shared singleton (`Error.None`, `Error.NullValue`) does not affect the singleton.
- `AddFieldError(field, messages)` creates the field entry even if `messages` is empty, and
  appends to (rather than replaces) an existing entry for the same field.
- `WithMetadata` attaches free-form key/value data (e.g. a retry-after hint or an entity id) and
  also returns a new `Error`. Setting an existing key overwrites its value; the dictionary
  overload merges into the existing metadata.
- `GetRootCause()` walks down the chain while each node has exactly one cause; it stops (and
  returns that node) once it reaches zero causes or more than one (an aggregation point).
- Equality is structural: `Code`, `Description`, `Type`, `Fields` (by content, message lists in order), `Metadata` (by
  content, values compared with `Equals`), and `Causes` (in order) must all match.

```csharp
var error = new Error("User.NotFound", "User was not found", ErrorType.NotFound);

var validation = new Error("Validation", "One or more fields are invalid")
    .AddFieldError("Email", "Email is required");

var wrapped = new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected)
    .CausedBy(new Error("Db.Timeout", "The database connection timed out."));

var limited = new Error("Rate.Limited", "Too many requests.", ErrorType.TooManyRequests)
    .WithMetadata("retryAfterSeconds", 30);
```

## `ErrorType`

A `readonly record struct` categorizing an `Error` by name, compared by value.

```csharp
[JsonConverter(typeof(ErrorTypeJsonConverter))]
public readonly record struct ErrorType
{
    public string Value { get; }

    public ErrorType(string value);   // throws InvalidErrorTypeException if value is null or empty

    public static readonly ErrorType None;
    public static readonly ErrorType NullValue;
    public static readonly ErrorType Validation;
    public static readonly ErrorType NotFound;
    public static readonly ErrorType BadRequest;
    public static readonly ErrorType Unauthorized;
    public static readonly ErrorType Forbidden;
    public static readonly ErrorType Conflict;
    public static readonly ErrorType TooManyRequests;
    public static readonly ErrorType Unexpected;
    public static readonly ErrorType ServiceUnavailable;
}
```

Declare your own categories the same way the built-ins are declared:

```csharp
public static class OrderErrorType
{
    public static readonly ErrorType OutOfStock = new(nameof(OutOfStock));
}
```

Serializes (via `System.Text.Json`) as its flat `Value` string rather than as a nested object.

## `ExceptionalError`

A `sealed record ExceptionalError : Error` built from a caught `Exception`.

```csharp
public sealed record ExceptionalError : Error
{
    public Exception Exception { get; }

    public static ExceptionalError From(Exception exception);
    public static Error From(Exception exception, Error error);
}
```

- `From(exception)` sets `Code` to the exception's type name and `Description` to
  `exception.Message` (`Type` is `ErrorType.Unexpected`). If the exception has an
  `InnerException`, it's recursively wrapped and attached via `CausedBy`.
- `From(exception, error)` wraps the exception (via `From(exception)`) as `error`'s
  `InnerError` — `error`'s own `Code`/`Description` are what's surfaced, keeping the raw
  exception message out of the primary description. Returns `Error`, not `ExceptionalError`,
  since the returned value's outer shape is `error`, not the wrapped exception.

See [Wrapping caught exceptions](../guides/wrapping-exceptions.md) for when to use which, and
[Result and Result\<TValue\>](result.md) for `Result.Failure(exception[, error])`, which builds
on these.

## Related

- [Building structured errors](../guides/building-structured-errors.md)
- [The Result and Error model](../concepts/result-and-error-model.md)
- [Exceptions](exceptions.md) — `InvalidErrorTypeException` and friends.

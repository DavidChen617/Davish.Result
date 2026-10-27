---
title: Building structured errors
weight: 30
---

## Field-level (validation) errors

`Error.Fields` holds messages keyed by field name. Build it fluently with `AddFieldError`:

```csharp
var error = new Error("Validation", "One or more fields are invalid")
    .AddFieldError("Email", "Email is required")
    .AddFieldError("Password", new[] { "Too short", "Must contain a digit" });

// error.Fields["Password"] => ["Too short", "Must contain a digit"]
```

Calling `AddFieldError` again for the same field name appends to the existing messages rather
than replacing them. `AddFieldError` never modifies the `Error` it's called on — it returns a
new `Error` with the message added. This matters if you call it on a shared instance like
`Error.NullValue`: the shared singleton is unaffected, and you must use the returned value.

## Metadata

`WithMetadata` attaches free-form key/value data to an error — handy for context that doesn't
fit `Code`/`Description`, such as an entity id or a retry hint:

```csharp
var error = new Error("Rate.Limited", "Too many requests.", ErrorType.TooManyRequests)
    .WithMetadata("retryAfterSeconds", 30);

error.Metadata["retryAfterSeconds"];   // 30
```

Like `AddFieldError`, it returns a new `Error` rather than modifying the receiver. Setting the
same key again overwrites it. Metadata takes part in equality, but the default failure handler
does not write it into the HTTP response.

## Cause chains

`CausedBy` attaches one or more underlying errors — the same idea as
`Exception.InnerException` (or `AggregateException.InnerExceptions` for more than one):

```csharp
var dbError = new Error("Db.Timeout", "The database connection timed out.");
var error = new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected)
    .CausedBy(dbError);

error.InnerError;      // dbError — the first cause (or null if there are none)
error.GetRootCause();   // walks down single-cause chains to the deepest error;
                        // stops at a node with zero or multiple causes (an aggregation point)
```

`CausedBy` never modifies the `Error` it's called on — like `AddFieldError`, it returns a new
`Error`. Calling it multiple times accumulates causes in the order added.

## Custom error types

Declare application-specific categories as `static readonly ErrorType` values, the same way the
built-in ones are declared:

```csharp
public static class OrderErrorType
{
    public static readonly ErrorType OutOfStock = new(nameof(OutOfStock));
    public static readonly ErrorType PaymentDeclined = new(nameof(PaymentDeclined));
}

var error = new Error("Order.OutOfStock", "Item is out of stock", OrderErrorType.OutOfStock);
```

`ErrorType` is a `record struct`, so it compares by its name — `new ErrorType("NotFound")` is
equal to `ErrorType.NotFound` regardless of which one you build. Pick names that don't
accidentally collide with the built-in categories unless that's what you want. See
[Configuring status codes](configuring-status-codes.md) to map a custom category to an HTTP
status code.

## Related

- [The Result and Error model](../concepts/result-and-error-model.md) — why `Error` is
  immutable and how equality works.
- [Error, ErrorType, and ExceptionalError](../reference/error.md) — full API.

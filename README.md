# Davish.Result

A lightweight **Result pattern** for .NET. Model success and failure as values instead of
throwing exceptions for expected error flows, and compose your operations into a single,
short-circuiting pipeline with `Then` (sync and async).

- **Explicit outcomes** — `Result` and `Result<TValue>` make "this can fail" part of the type.
- **Structured errors** — `Error` carries a code, description, category, and per-field messages.
- **Extensible categories** — a set of built-in `ErrorType` values, extend it with your own.
- **Fluent composition** — chain steps with `Then` / `ThenAsync`; a failure skips the rest.
- **Minimal API integration** — convert a `Result` straight into an `IResult` with a configurable error-to-status-code mapping.
- **Broad reach** — `Davish.Result`/`Davish.Result.Extension` target `netstandard2.0` and `net10.0`.

## Installation

```bash
dotnet add package Davish.Result
dotnet add package Davish.Result.Extension          # Then / ThenAsync composition
dotnet add package Davish.Result.AspNetCore.Http    # Result -> Minimal API IResult (net10.0)
```

Everything lives in a single namespace:

```csharp
using Davish.Result;
```

## Quick start

```csharp
Result<OrderConfirmation> confirmation = await ValidateRequest(request)
    .ThenAsync(CheckStockAsync)
    .ThenAsync(ChargeAsync)
    .ThenAsync(CreateOrderAsync)
    .Then(Confirm);

string message = confirmation.IsSuccess
    ? $"Order {confirmation.Value.OrderId} placed"
    : confirmation.Error.Description;
```

Each step runs only if the previous one succeeded. The first failure short-circuits the
rest of the chain and flows through untouched.

## Usage

### Creating results

```csharp
Result ok = Result.Success();
Result bad = Result.Failure(new Error("User.Locked", "The account is locked"));

Result<int> value = Result.Success(42);
Result<int> failed = Result.Failure<int>(new Error("Parse.Failed", "Not a number"));
```

### Reading a result

```csharp
if (result.IsSuccess)
    Use(result.Value);           // Result<T>.Value throws ResultValueUnavailableException if the result is a failure
else
    Log(result.Error.Description);
```

> [!NOTE]
> `ResultValueUnavailableException` and `InvalidResultStateException` (thrown by `Result.Failure(Error.None)`
> and similar invalid combinations) both derive from `ResultException`, so `catch (ResultException)` handles either.

### Implicit conversions

A value or an `Error` converts to a result implicitly, so factory methods can just `return`:

```csharp
Result<string> GetName(int id)
{
    if (id <= 0)
        return new Error("Id.Invalid", "Id must be positive");   // Error  -> failed result

    return "David";                                              // value  -> successful result
}
```

> [!NOTE]
> A `null` value converts to a failure carrying `Error.NullValue`.

### Composing with `Then`

`Then` maps or binds the successful value; `ThenAsync` does the same for asynchronous steps.
Mix them freely — the chain stays flat.

```csharp
Result<string> result = Result.Success(2)
    .Then(x => x + 3)                      // map:  int -> int
    .Then(x => Result.Success(x * 10))     // bind: int -> Result<int>
    .Then(x => x.ToString());              // map:  int -> string

Result<Profile> profile = await FetchUserAsync(id)   // Task<Result<User>>
    .ThenAsync(LoadProfileAsync)                      // async bind
    .Then(p => p.WithDefaults());                     // sync map on the awaited result
```

> [!TIP]
> Use `ThenAsync` for `async` steps. `Then` only accepts synchronous delegates — passing an
> `async` lambda to `Then` compiles to a result wrapping an un-awaited `Task`, which is almost
> never what you want.

### Errors

`Error` describes what went wrong and is categorized by an `ErrorType`:

```csharp
var error = new Error("User.NotFound", "User was not found", ErrorType.NotFound);
```

Built-in categories:

| Category | Meaning |
| --- | --- |
| `None` | No error (used by successful results) |
| `NullValue` | A `null` value was provided |
| `Validation` | Validation failure (default for `new Error(code, description)`) |
| `NotFound` | Resource not found |
| `BadRequest` | Malformed or invalid request |
| `Unauthorized` | Caller is not authenticated |
| `Forbidden` | Caller is authenticated but not allowed |
| `Conflict` | Conflict, such as a duplicate or concurrency violation |
| `Unexpected` | Unexpected, unhandled error |
| `ServiceUnavailable` | Downstream service is unavailable |

> [!NOTE]
> `ErrorType` uses reference equality, so each declared category is a distinct singleton.

### Field-level (validation) errors

`Error.Fields` holds messages keyed by field name. Build it fluently with `AddFieldError`:

```csharp
var error = new Error("Validation", "One or more fields are invalid")
    .AddFieldError("Email", "Email is required")
    .AddFieldError("Password", ["Too short", "Must contain a digit"]);

// error.Fields["Password"] => ["Too short", "Must contain a digit"]
```

### Custom error types

Extend `ErrorType` to define application-specific categories, using the same static-factory pattern:

```csharp
public sealed class OrderErrorType : ErrorType
{
    public static readonly OrderErrorType OutOfStock = new(nameof(OutOfStock));
    public static readonly OrderErrorType PaymentDeclined = new(nameof(PaymentDeclined));

    private OrderErrorType(string name) : base(name) { }
}

var error = new Error("Order.OutOfStock", "Item is out of stock", OrderErrorType.OutOfStock);
```

## ASP.NET Core / Minimal APIs

`Davish.Result.AspNetCore.Http` converts a `Result`/`Result<T>` straight into a Minimal API `IResult`:
a success maps to the corresponding 2xx, a failure maps to a `ProblemDetails` (or a validation
problem, if `Error.Fields` is populated).

```csharp
app.MapGet("/bookings/{id}", (int id, BookingService service) =>
    service.Find(id).ToOk());                              // 200 OK, or a problem result

app.MapPost("/bookings", (CreateBooking request, BookingService service) =>
    service.Create(request).ToCreated("GetBooking", b => new { id = b.Id }));  // 201 Created

app.MapDelete("/bookings/{id}", (int id, BookingService service) =>
    service.Delete(id).ToNoContent());                     // 204 No Content, or a problem result
```

| Method | Success | Failure |
| --- | --- | --- |
| `ToOk()` | `200 OK` | problem result |
| `ToNoContent()` | `204 No Content` | problem result |
| `ToCreated(routeName, routeValues)` | `201 Created` | problem result |
| `ToAccepted(uri)` | `202 Accepted` | problem result |
| `ToProblemDetail()` | — | problem, or validation problem if `Error.Fields` is populated |
| `ToValidationProblemDetail()` | — | validation problem from `Error.Fields` |

### Error type → status code mapping

Failures map to a status code by `Error.Type`. Built-in categories map as you'd expect
(`Validation`/`NullValue`/`BadRequest` → 400, `NotFound` → 404, `Unauthorized` → 401,
`Forbidden` → 403, `Conflict` → 409, `ServiceUnavailable` → 503, `Unexpected` and anything
unregistered → 500). Register your own categories once at startup:

```csharp
builder.Services.AddCustomResultErrorTypeMap(v =>
{
    // v.UseDefault = false;   // opt out of the built-in mappings above

    v.CustomMap = new Dictionary<ErrorTypeBase, int>
    {
        [OrderErrorType.OutOfStock] = StatusCodes.Status409Conflict,
        [OrderErrorType.PaymentDeclined] = StatusCodes.Status402PaymentRequired,
    };
});
```

> [!NOTE]
> Error types are matched by reference, not by `Name` — two distinct `ErrorType` instances that
> happen to share the same name are treated as different categories. Reuse the same
> `static readonly` instance both when constructing the `Error` and when mapping it here.

> [!IMPORTANT]
> Configure this once at startup, before the app serves any requests. The mapping locks itself
> the first time a status code is resolved — reconfiguring afterward throws `ResultHttpOptionsLockedException`.

# Davish.Result

A lightweight **Result pattern** for .NET. Model success and failure as values instead of
throwing exceptions for expected error flows, and compose your operations into a single,
short-circuiting pipeline with `Then` (sync and async).

- **Explicit outcomes** — `Result` and `Result<TValue>` make "this can fail" part of the type.
- **Structured errors** — `Error` carries a code, description, category, per-field messages,
  free-form metadata, and an optional cause chain (`CausedBy`/`InnerError`/`GetRootCause`,
  mirroring `Exception.InnerException`).
- **Extensible categories** — a set of built-in `ErrorType` values, extend it with your own.
- **Fluent composition** — chain steps with `Then` / `ThenAsync`; a failure skips the rest.
- **Exception bridging** — `Result.Failure(exception)` wraps a caught exception as an `Error`
  (`ExceptionalError`), without leaking its message to a client unless you choose to.
- **Minimal API integration** — convert a `Result` straight into an `IResult`, with pluggable
  failure handling and exception-to-`Result` mapping.
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

Both types also support deconstruction:

```csharp
var (isSuccess, error) = result;                  // Result
var (isSuccess, value, error) = typedResult;       // Result<TValue> — value is default on failure, never throws
```

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
| `TooManyRequests` | Caller is rate-limited (sent too many requests) |
| `Unexpected` | Unexpected, unhandled error |
| `ServiceUnavailable` | Downstream service is unavailable |

> [!NOTE]
> `ErrorType` is a `record struct` wrapping its name, so it uses value equality — two `ErrorType`
> values with the same name are the same category, however they were constructed.

### Field-level (validation) errors

`Error.Fields` holds messages keyed by field name. Build it fluently with `AddFieldError`:

```csharp
var error = new Error("Validation", "One or more fields are invalid")
    .AddFieldError("Email", "Email is required")
    .AddFieldError("Password", ["Too short", "Must contain a digit"]);

// error.Fields["Password"] => ["Too short", "Must contain a digit"]
```

> [!NOTE]
> `AddFieldError` never modifies the `Error` it's called on — it returns a new `Error` with the
> message added. This matters if you call it on a shared instance like `Error.NullValue`: the
> shared singleton is unaffected, and you must use the returned value.

### Metadata

`Error.Metadata` holds free-form, application-specific data keyed by name — unlike `Fields`, values
aren't restricted to validation messages. Build it fluently with `WithMetadata`:

```csharp
var error = new Error("Order.LoadFailed", "Could not load the order")
    .WithMetadata("OrderId", orderId)
    .WithMetadata(new Dictionary<string, object?> { ["RetryCount"] = 3, ["Region"] = "us-east-1" });

// error.Metadata["OrderId"] => orderId
```

> [!NOTE]
> `WithMetadata` never modifies the `Error` it's called on — like `AddFieldError`, it returns a new
> `Error`. Unlike `Fields`, adding the same key twice overwrites the previous value rather than
> accumulating.

### Cause chains

`CausedBy` attaches one or more underlying errors, the same idea as `Exception.InnerException`
(or `AggregateException.InnerExceptions` for more than one):

```csharp
var dbError = new Error("Db.Timeout", "The database connection timed out.");
var error = new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected)
    .CausedBy(dbError);

error.InnerError;      // dbError — the first cause (or null if there are none)
error.GetRootCause();   // walks down single-cause chains to the deepest error;
                        // stops at a node with zero or multiple causes (an aggregation point)
```

> [!NOTE]
> `CausedBy` never modifies the `Error` it's called on — like `AddFieldError`, it returns a new `Error`.

### Custom error types

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

### Wrapping caught exceptions

`ExceptionalError` wraps a caught exception as an `Error`, mapping its `InnerException` chain onto
`Error.Causes`:

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

> [!WARNING]
> `Result.Failure(exception)` sets `Error.Description` to the raw `Exception.Message`, which can
> contain implementation details (SQL fragments, file paths, internal type names) you may not want
> an untrusted client to see. If the result could reach an HTTP response, use
> `Result.Failure(exception, error)` instead: it surfaces the `Error` you provide, with the
> exception attached only as its `InnerError`.

```csharp
catch (Exception ex)
{
    return Result.Failure<Order>(ex, new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected));
}

// result.Error.Description               -> "Could not load the order."  (safe to expose)
// result.Error.InnerError.Description     -> the raw ex.Message           (not exposed unless you read it)
```

To get the original exception back for logging, pattern-match on `ExceptionalError` — `result.Error`
itself when using `Result.Failure(exception)`, or `result.Error.InnerError` when using
`Result.Failure(exception, error)`:

```csharp
if (result.Error.InnerError is ExceptionalError { Exception: var ex })
    logger.LogError(ex, "Operation failed");   // full stack trace, exception type, etc.
```

## ASP.NET Core / Minimal APIs

`Davish.Result.AspNetCore.Http` converts a `Result`/`Result<T>` straight into a Minimal API `IResult`:
a success maps to the corresponding 2xx, a failure is deferred to a pluggable failure handler
(`ProblemDetails`, or a validation problem, by default). It also lets you map exceptions your own
code doesn't explicitly catch into the same `Result` failure pipeline.

### Setup

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.AddMinimalApiResult();   // registers ToOk()/ToCreated()/... failure handling — skip only if you never use them
});

var app = builder.Build();

app.UseExceptionHandler();   // required if you use MapExceptionToResult (see below)
```

### Converting a `Result` into a response

```csharp
app.MapGet("/bookings/{id}", (int id, BookingService service) =>
    service.Find(id).ToOk());                              // 200 OK, or the failure handler's response

app.MapPost("/bookings", (CreateBooking request, BookingService service) =>
    service.Create(request).ToCreated("GetBooking", b => new { id = b.Id }));  // 201 Created

app.MapDelete("/bookings/{id}", (int id, BookingService service) =>
    service.Delete(id).ToNoContent());                     // 204 No Content, or the failure handler's response
```

| Method | Success |
| --- | --- |
| `ToOk()` | `200 OK` |
| `ToNoContent()` | `204 No Content` |
| `ToCreated(routeName, routeValues)` | `201 Created` at a named route |
| `ToCreated()` | `201 Created`, no `Location` (non-generic only) |
| `ToCreatedAtLocation(string \| Uri)` | `201 Created` with an explicit `Location` |
| `ToCreatedAtLocation(value => string \| Uri)` | same, `Location` built from the value (generic only) |
| `ToAccepted(string \| Uri)` | `202 Accepted` with an optional `Location` |
| `ToAcceptedAtRoute(routeName, routeValues)` | `202 Accepted` at a named route |
| `ToText(...)` / `ToContent(...)` | `200 OK` (or the given `statusCode`) with a text body |
| `ToJson(options \| context \| typeInfo)` | `200 OK` (or the given `statusCode`) serializing the value with an explicit serializer (generic only) |
| `ToServerSentEvents(...)` | `200 OK` `text/event-stream`, on `Result<IAsyncEnumerable<string \| T \| SseItem<T>>>` |
| `ToBytes(...)` / `ToFile(...)` / `ToPhysicalFile(...)` / `ToVirtualFile(...)` / `ToStream(...)` | `200 OK` file or stream response; generic overloads take a `Func<T, ...>` for the content or path |
| `ToRedirect(...)` / `ToLocalRedirect(...)` / `ToRedirectToRoute(...)` | `301`/`302`/`307`/`308` depending on `permanent` and `preserveMethod` |
| `ToSignIn(...)` / `ToSignOut(...)` | sign in / out via the authentication handler (`StatusCode` is `null`: the handler decides) |
| `ToChallenge(...)` / `ToForbid(...)` | challenge / forbid via the authentication handler, e.g. to start an external (OAuth/OIDC) login (`StatusCode` is `null`: the handler decides) |
| `ToStatus(statusCode)` | a bare response with the given status code |

Most have a non-generic (`Result`) and generic (`Result<T>`, carrying the value) overload; the ones marked
above are only meaningful on one side. For `ToText`/`ToContent` the generic overloads take a `Func<T, string>`
that builds the body from the value. `ToCreatedAtLocation` is named differently from `ToCreated(routeName, ...)`
because a `string` overload of the latter name would be indistinguishable from it.
Where the real status is only decided at execution time, the shape's `StatusCode` property is `null` (unknown) on
success rather than a guess: `ToSignIn`/`ToSignOut`/`ToChallenge`/`ToForbid` (the authentication handler decides, e.g. `302`
for a cookie handler), and the file/stream shapes when range processing or an `ETag`/`Last-Modified` is requested
(`206`/`304`/`412` are possible). Everywhere else it is exact. The response itself always matches the built-in
`TypedResults`; only the property is affected. `ToChallenge`/`ToForbid` hand the response to the authentication handler (a redirect to a login page, `WWW-Authenticate`, ...),
unlike a failed result mapped from `ErrorType.Unauthorized`/`Forbidden`, which always produces `ProblemDetails`. Failure
is always handled the same way, by the registered `IMinimalApiFailureHandler` — see below.

> [!NOTE]
> `ToOk()` etc. return a concrete public type (`MinimalApiOkResult<T>` and friends), not `IResult`,
> so ASP.NET Core's OpenAPI generation can describe the success response correctly. The base types
> (`MinimalApiResult`/`MinimalApiResult<TValue>`) are public too, so you can add your own success
> shapes (e.g. `206 Partial Content`) by inheriting from them and reusing their failure handling.

### Customizing failure handling

The default `IMinimalApiFailureHandler` produces a `ProblemDetails` (or validation problem, if
`Error.Fields` is populated) using the status code mapping below. Replace it with your own:

```csharp
public sealed class MyFailureHandler : IMinimalApiFailureHandler
{
    public ValueTask<IResult> HandleAsync(Result result, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Results.Json(new { error = result.Error.Code }));
}

builder.Services.AddResultAspNetCore(o =>
{
    o.AddMinimalApiResult(m => m.MinimalApiFailureHandler<MyFailureHandler>());
});
```

### Mapping exceptions into `Result`

For code that throws instead of returning a `Result` (a third-party library, for example),
`MapExceptionToResult` converts a caught exception into the same failure pipeline as `ToOk()`:

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.MapExceptionToResult<BookingLockedException>((context, exception) =>
        Result.Failure(new Error("Booking.Locked", "This booking is already confirmed.", ErrorType.Conflict)));

    o.AddMinimalApiResult();
});
```

Matching is by inheritance — registering a mapper for a base exception type also matches its
subclasses, most-specific match wins. `MapExceptionToResult<TException, TMapper>()` (implementing
`IExceptionMapper<TException>`) is also available for mappers that need dependency injection.

> [!IMPORTANT]
> `app.UseExceptionHandler()` is required for this to take effect — `AddResultAspNetCore` only
> registers the DI services, it doesn't wire up the middleware pipeline.

### Error type → status code mapping

Failures map to a status code by `Error.Type`. Built-in categories map as you'd expect
(`Validation`/`NullValue`/`BadRequest` → 400, `NotFound` → 404, `Unauthorized` → 401,
`Forbidden` → 403, `Conflict` → 409, `TooManyRequests` → 429, `ServiceUnavailable` → 503,
`Unexpected` and anything unregistered → 500). Register your own categories from the same
`AddResultAspNetCore` call as everything else above, via `o.ConfigureStatusCodes(...)`:

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.ConfigureStatusCodes(v =>
    {
        // v.UseDefault = false;   // opt out of the built-in mappings above

        v.CustomMap = new Dictionary<ErrorType, int>
        {
            [OrderErrorType.OutOfStock] = StatusCodes.Status409Conflict,
            [OrderErrorType.PaymentDeclined] = StatusCodes.Status402PaymentRequired,
        };
    });
});
```

This is process-wide static configuration, applied immediately rather than resolved from a DI
container — the same idea as Dapper's `SqlMapper.Settings`: a plain, freely reconfigurable global,
not a "configure once and freeze" builder. Since `ErrorType` uses value equality, `OrderErrorType.OutOfStock`
and any other `ErrorType` you build with the same name map to the same entry.

> [!NOTE]
> Calling `ConfigureStatusCodes` again — even after status codes have already been resolved for
> real requests — replaces the mapping wholesale; there's no lock to fight. This matters for tests
> that spin up multiple hosts in one process (e.g. a fresh `WebApplicationFactory` per test): each
> host's own `AddResultAspNetCore` call just reapplies its mapping, no special handling needed.
> The flip side is the usual one for any process-wide static: if two hosts in the same process want
> *different* mappings at the same time, whichever configures last wins for both — keep the mapping
> identical across hosts that share a process, the way you already would for `SqlMapper.Settings`.

### Customizing ProblemDetails

`o.ConfigureProblemDetails(...)` (also from `AddResultAspNetCore`) configures the same
`ProblemDetailsOptions` ASP.NET Core's `[ApiController]`/`ControllerBase.Problem()` use — it's not
specific to Minimal APIs:

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.ConfigureProblemDetails(p => p.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
});
```

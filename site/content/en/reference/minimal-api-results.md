---
title: Minimal API results
weight: 50
---

Namespace: `Davish.Result` (package `Davish.Result.AspNetCore.Http`). Requires
[ASP.NET Core setup](aspnetcore-setup.md) to be registered.

## `ResultToMinimalResultExtension`

Core success shapes (full parameter lists are in the `ResultToMinimalResultExtension` XML docs):

```csharp
public static MinimalApiOkResult ToOk(this Result result);
public static MinimalApiNoContentResult ToNoContent(this Result result);
public static MinimalApiCreatedResult ToCreated(this Result result, string routeName, object? routeValues = null);
public static MinimalApiAcceptedResult ToAccepted(this Result result, string? uri = null);

public static MinimalApiOkResult<T> ToOk<T>(this Result<T> result) where T : notnull;
public static MinimalApiNoContentResult ToNoContent<T>(this Result<T> result) where T : notnull;
public static MinimalApiCreatedResult<T> ToCreated<T>(this Result<T> result, string routeName, Func<T, object?>? routeValues = null) where T : notnull;
public static MinimalApiAcceptedResult<T> ToAccepted<T>(this Result<T> result, string? uri = null) where T : notnull;
```

| Method | Success status | Notes |
|---|---|---|
| `ToOk()` | `200 OK` | Generic overload writes the value as the response body. |
| `ToNoContent()` | `204 No Content` | Same type for both generic and non-generic — a `204` never has a body. |
| `ToCreated(routeName, routeValues)` | `201 Created` | Builds the `Location` header via `TypedResults.CreatedAtRoute`. Generic overload's `routeValues` is `Func<T, object?>`, built from the success value. |
| `ToAccepted(uri)` | `202 Accepted` | `uri` is optional; when given, becomes the `Location` header. |

Additional shapes, each mirroring the `TypedResults` factory of the same name:

| Group | Methods | Result type |
|---|---|---|
| Created / Accepted variants | `ToCreated()`, `ToCreatedAtLocation(string \| Uri \| Func<T, string \| Uri>)`, `ToCreated(routeName, RouteValueDictionary)`, `ToAccepted(Uri)`, `ToAcceptedAtRoute(...)` | `MinimalApiCreatedAtLocationResult[<T>]`, `MinimalApiCreatedResult[<T>]`, `MinimalApiAcceptedResult[<T>]`, `MinimalApiAcceptedAtRouteResult[<T>]` |
| Text / content | `ToText(...)`, `ToContent(...)` | `MinimalApiContentResult[<T>]` |
| JSON | `ToJson(options \| JsonSerializerContext \| JsonTypeInfo<T>)` (generic only) | `MinimalApiJsonResult<T>` |
| Server-sent events | `ToServerSentEvents(...)` on `Result<IAsyncEnumerable<string \| T \| SseItem<T>>>` | `MinimalApiServerSentEventsResult<T>`, `MinimalApiSseItemResult<T>` |
| Files / streams | `ToBytes`, `ToFile`, `ToPhysicalFile`, `ToVirtualFile`, `ToStream` | `MinimalApiFileResult[<T>]` |
| Redirects | `ToRedirect`, `ToLocalRedirect`, `ToRedirectToRoute` | `MinimalApiRedirectResult[<T>]` |
| Authentication | `ToSignIn`, `ToSignOut`, `ToChallenge`, `ToForbid` | `MinimalApiAuthenticationResult[<T>]` |
| Bare status | `ToStatus(int statusCode)` | `MinimalApiStatusCodeResult` |

`StatusCode` is `null` on success for the authentication shapes and for file/stream shapes when
range processing or an `ETag`/`Last-Modified` is requested, because the real status is only
decided at execution time. See
[Converting results to HTTP responses](../guides/converting-results-to-http-responses.md).

Failure always defers to the registered `IMinimalApiFailureHandler` (see below), for every
method above.

## `MinimalApiResult` / `MinimalApiResult<TValue>`

Public abstract base classes implementing `IResult`. Inherit from these to add your own success
shape while reusing the built-in failure handling.

```csharp
public abstract class MinimalApiResult : IResult, IStatusCodeHttpResult
{
    protected MinimalApiResult(Result result);
    protected abstract int SuccessStatusCode { get; }
    protected abstract IResult CreateSuccessResult();
    public int? StatusCode { get; }   // SuccessStatusCode on success, Error.Type.ToStatusCode() on failure
    public Task ExecuteAsync(HttpContext httpContext);
}

public abstract class MinimalApiResult<TValue> : IResult, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<TValue>
    where TValue : notnull
{
    protected MinimalApiResult(Result<TValue> result);
    protected abstract int SuccessStatusCode { get; }
    protected abstract IResult CreateSuccessResult(TValue value);
    public int? StatusCode { get; }
    public Task ExecuteAsync(HttpContext httpContext);
    // IValueHttpResult<TValue>.Value throws ResultValueUnavailableException on failure
    // IValueHttpResult.Value returns null on failure instead of throwing
}
```

The built-in shapes returned by the `ToXxx()` methods above are `public sealed` implementations
of these base classes; you don't construct them directly — use the `ToXxx()` extension methods.

## `IFailureHandler<TResult>` / `IMinimalApiFailureHandler`

```csharp
public interface IFailureHandler<TResult>
{
    ValueTask<TResult> HandleAsync(Result result, CancellationToken cancellationToken);
}

public interface IMinimalApiFailureHandler : IFailureHandler<IResult>;
```

`HandleAsync` is only ever called for a failed `Result`. Implement `IMinimalApiFailureHandler`
and register it via `MinimalApiResultOptions.MinimalApiFailureHandler<THandler>()` to replace
the default handler — see
[Customizing failure handling](../guides/customizing-failure-handling.md).

The default implementation (`DefaultMinimalApiFailureHandler`, internal) produces a
`TypedResults.Problem(...)` using `error.Type.ToStatusCode()`, or
`TypedResults.ValidationProblem(...)` if `Error.Fields` is non-empty (using
`error.Fields` as the `errors` dictionary). `Error.Causes`/`InnerError` never appear in the
response body.

## Related

- [Converting results to HTTP responses](../guides/converting-results-to-http-responses.md)
- [Customizing failure handling](../guides/customizing-failure-handling.md)
- [ASP.NET Core integration model](../concepts/aspnetcore-integration-model.md)

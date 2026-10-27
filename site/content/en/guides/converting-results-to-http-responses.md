---
title: Converting results to HTTP responses
weight: 60
---

Assumes the ASP.NET Core integration is already set up — see
[Setting up the ASP.NET Core integration](setting-up-aspnetcore-integration.md).

## Basic conversions

```csharp
app.MapGet("/bookings/{id}", (int id, BookingService service) =>
    service.Find(id).ToOk());                              // 200 OK, or the failure handler's response

app.MapPost("/bookings", (CreateBooking request, BookingService service) =>
    service.Create(request).ToCreated("GetBooking", b => new { id = b.Id }));  // 201 Created

app.MapDelete("/bookings/{id}", (int id, BookingService service) =>
    service.Delete(id).ToNoContent());                     // 204 No Content, or the failure handler's response
```

| Method | Success |
|---|---|
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
| `ToSignIn(...)` / `ToSignOut(...)` | sign in / out via the authentication handler |
| `ToChallenge(...)` / `ToForbid(...)` | challenge / forbid via the authentication handler |
| `ToStatus(statusCode)` | a bare response with the given status code |

Most have a non-generic (`Result`) and generic (`Result<T>`, carrying the value) overload; the
ones marked above are only meaningful on one side. For `ToText`/`ToContent` the generic
overloads take a `Func<T, string>` that builds the body from the value.
`ToCreatedAtLocation` is named differently from `ToCreated(routeName, ...)` because a `string`
overload of the latter name would be indistinguishable from it.

Each shape mirrors the matching `TypedResults` factory, so the response itself is identical to
the built-in one. Where the real status is only decided at execution time, the shape's
`StatusCode` property is `null` (unknown) on success rather than a guess: `ToSignIn`/`ToSignOut`/
`ToChallenge`/`ToForbid` (the authentication handler decides, e.g. `302` for a cookie handler),
and the file/stream shapes when range processing or an `ETag`/`Last-Modified` is requested
(`206`/`304`/`412` are possible). Everywhere else it is exact.

`ToChallenge`/`ToForbid` hand the response to the authentication handler (a redirect to a login
page, `WWW-Authenticate`, ...), unlike a failed result mapped from `ErrorType.Unauthorized`/
`Forbidden`, which is always handled by the registered `IMinimalApiFailureHandler` (the default
handler produces `ProblemDetails`).

## Success is written immediately; failure is deferred to the registered handler

`ToOk()` etc. return a concrete public type (`MinimalApiOkResult<T>` and friends), not
`IResult`, so ASP.NET Core's OpenAPI generation can describe the success response correctly.
The base types (`MinimalApiResult`/`MinimalApiResult<TValue>`) are public too, so you can add
your own success shapes (e.g. `206 Partial Content`) by inheriting from them and reusing their
failure handling.

## Reading the value off a generic conversion

`MinimalApiResult<TValue>` implements `IValueHttpResult<TValue>` (throws
`ResultValueUnavailableException` on failure, like `Result<TValue>.Value` itself) and the
non-generic `IValueHttpResult` (returns `null` on failure instead of throwing). Check
`StatusCode`/`IsSuccess` on the underlying result before reading the value if you need it
outside of `ExecuteAsync`.

## Related

- [ASP.NET Core integration model](../concepts/aspnetcore-integration-model.md) — why success
  is written immediately while failure is deferred, and why the return types are concrete.
- [Minimal API results](../reference/minimal-api-results.md) — full signatures.

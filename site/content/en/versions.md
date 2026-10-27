---
title: Versions
weight: 5
---

Current package versions, and the changes that matter when upgrading.

| Package | Version |
|---|---|
| `Davish.Result` | 3.1.0 |
| `Davish.Result.AspNetCore.Http` | 4.0.0 |

## Davish.Result.AspNetCore.Http

### 4.0.0 (breaking)

- Removed the "configure once" lock on the status code mapping, along with
  `ResultHttpOptionsLockedException`. `ConfigureStatusCodes` can now be called repeatedly.
- `ResultHttpOptions.Configure` is now `internal`. Use `ConfigureStatusCodes` inside
  `AddResultAspNetCore`.

### After 4.0.0 (version not yet bumped)

- Added the remaining `TypedResults` success shapes: `ToCreatedAtLocation`, `ToAcceptedAtRoute`,
  `ToText`/`ToContent`, `ToJson`, `ToServerSentEvents`, file and stream results, redirects,
  authentication results, and `ToStatus`.

### 3.1.0

- Added `ErrorType.TooManyRequests`, mapped to `429` by default.

### 3.0.0

- Added exception mapping (`MapExceptionToResult`), pluggable failure handling (`IMinimalApiFailureHandler`) and `ConfigureProblemDetails`.

## Davish.Result

### 3.1.0

- Added `Error.Metadata` and `Error.WithMetadata(...)`.
- Added `ErrorType.TooManyRequests`.

### 3.0.0

- `Error` is now immutable. Added `ExceptionalError`, `Error.Causes`/`InnerError`/`GetRootCause()`, and `Result` deconstruction.

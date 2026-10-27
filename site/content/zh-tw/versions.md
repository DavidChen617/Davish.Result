---
title: 版本
weight: 5
---

目前的套件版本，以及升級時需要留意的變更。

| 套件 | 版本 |
|---|---|
| `Davish.Result` | 3.1.0 |
| `Davish.Result.AspNetCore.Http` | 4.0.0 |

## Davish.Result.AspNetCore.Http

### 4.0.0（breaking）

- 移除狀態碼對應表「只能設定一次」的鎖，以及 `ResultHttpOptionsLockedException`。
  `ConfigureStatusCodes` 現在可以重複呼叫。
- `ResultHttpOptions.Configure` 改為 `internal`。請在 `AddResultAspNetCore` 內使用
  `ConfigureStatusCodes`。

### 4.0.0 之後（版本號尚未調整）

- 補上其餘的 `TypedResults` 成功回應：`ToCreatedAtLocation`、`ToAcceptedAtRoute`、
  `ToText`/`ToContent`、`ToJson`、`ToServerSentEvents`、檔案與 stream、redirect、
  authentication 結果，以及 `ToStatus`。

### 3.1.0

- 新增 `ErrorType.TooManyRequests`，預設對應 `429`。

### 3.0.0

- 新增例外對應（`MapExceptionToResult`）、可替換的失敗處理（`IMinimalApiFailureHandler`）與 `ConfigureProblemDetails`。

## Davish.Result

### 3.1.0

- 新增 `Error.Metadata` 與 `Error.WithMetadata(...)`。
- 新增 `ErrorType.TooManyRequests`。

### 3.0.0

- `Error` 改為不可變。新增 `ExceptionalError`、`Error.Causes`/`InnerError`/`GetRootCause()`，以及 `Result` 解構。

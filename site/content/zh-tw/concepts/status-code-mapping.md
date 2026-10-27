---
title: 錯誤類型對應狀態碼
weight: 40
---

`ResultHttpOptions` 把每一個 `ErrorType` 對應到一個 HTTP 狀態碼。`DefaultMinimalApiFailureHandler`
以及 `MinimalApiResult.StatusCode`/`MinimalApiResult<TValue>.StatusCode` 都是透過
`ErrorType.ToStatusCode()` 來解析一個失敗結果的狀態碼，而這個方法會轉呼叫這個對應表。

## 內建的預設值

| `ErrorType` | 狀態碼 |
|---|---|
| `Validation` | 400 Bad Request |
| `NullValue` | 400 Bad Request |
| `BadRequest` | 400 Bad Request |
| `NotFound` | 404 Not Found |
| `Unauthorized` | 401 Unauthorized |
| `Forbidden` | 403 Forbidden |
| `Conflict` | 409 Conflict |
| `TooManyRequests` | 429 Too Many Requests |
| `ServiceUnavailable` | 503 Service Unavailable |
| `Unexpected` | 500 Internal Server Error |
| 沒有登記的類型 | 500 Internal Server Error |

## Process-wide 的靜態設定，不是走 DI

`ConfigureStatusCodes(...)` 會整份替換這個對應表，而且是立刻生效的靜態操作，不是
每個請求各自從 DI 容器解析出來的。程式裡的文件註解明確把這個模型拿來跟設定
`JsonSerializerOptions` 或 Dapper 的 `SqlMapper.Settings` 相比：一份 process-wide 的表。

因為 `ErrorType` 是依 `Value` 這個名字來比較的 `record struct`，你自己建立、名字跟
`OrderErrorType.OutOfStock` 相同的 `ErrorType`，會對應到表裡同一筆。比對的依據是名字，不是身分。

## 允許重新設定

`ResultHttpOptions.Configure` 是 `internal`，每次 `ConfigureStatusCodes` 呼叫都會換上一份全新的表，
沒有鎖。這對在同一個 process 裡
每個測試建立一個新 host 的情況很重要，因為每個 host 的 `AddResultAspNetCore` 只是重新套用它的
對應表。代價是任何 process-wide 靜態狀態都有的老問題：兩個想要不同對應表的 host 會互相覆蓋，
所以同一個 process 裡的各個 host 請保持相同的對應表。

## `ConfigureStatusCodes` 是唯一的公開入口

`ResultAspNetCoreOptions.ConfigureStatusCodes(Action<ResultHttpOptionsBuilder>)` 讓你能從跟其他
設定同一個 `AddResultAspNetCore(o => ...)` 呼叫裡設定這份對應表。

要註冊自訂的 `ErrorType` 對應，參考 [設定狀態碼](../guides/configuring-status-codes.md)；完整
API 參考 [狀態碼對應](../reference/status-code-mapping.md)。

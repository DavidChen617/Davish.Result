---
title: 錯誤類型對應狀態碼
weight: 40
---

`ResultHttpOptions` 決定每個 `ErrorType` 對應到哪個 HTTP 狀態碼。這份對應表是 process-wide 的
靜態設定，也就是整個 process 共用一份，不是透過 DI 注入。大多數應用程式不會感覺到差別；
但如果你在同一個 process 裡啟動多個 host（例如整合測試），就要知道它們共用同一份表，
詳見下方[允許重新設定](#允許重新設定)。

失敗結果的狀態碼都經由 `ErrorType.ToStatusCode()` 查這份表取得，`DefaultMinimalApiFailureHandler`
和 `MinimalApiResult.StatusCode`/`MinimalApiResult<TValue>.StatusCode` 都是這樣。

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

## 靜態設定，不走 DI

`ConfigureStatusCodes(...)` 會整份替換對應表，而且立刻生效，不是每個請求各自從 DI 容器解析。
原始碼的文件註解拿 Dapper 的 `SqlMapper.Settings` 來比喻：整個 process 共用一份表。

`ErrorType` 是依 `Value`（名字）比較的 `record struct`。你自己建立的 `ErrorType`，只要名字跟
`OrderErrorType.OutOfStock` 相同，就會對應到表裡同一筆。比對只看名字，不看是哪個實例。

## 允許重新設定

`ResultHttpOptions.Configure` 是 `internal`。每次呼叫 `ConfigureStatusCodes`，都會換上一份全新的表，
沒有加鎖。

這個設計是為了測試：在同一個 process 裡，每個測試各建一個新 host 時，每個 host 的
`AddResultAspNetCore` 只會重新套用自己的對應表，不會出錯。代價是所有 process-wide 靜態狀態
都有的老問題：如果兩個 host 想要不同的對應表，後設定的會蓋掉先設定的。所以同一個 process 裡的
host，請使用相同的對應表。

## `ConfigureStatusCodes` 是唯一的公開入口

`ResultAspNetCoreOptions.ConfigureStatusCodes(Action<ResultHttpOptionsBuilder>)` 讓你在
`AddResultAspNetCore(o => ...)` 裡，跟其他設定一起設定這份對應表。

要註冊自訂 `ErrorType` 的對應，參考[設定狀態碼](../guides/configuring-status-codes.md)；完整
API 參考[狀態碼對應](../reference/status-code-mapping.md)。

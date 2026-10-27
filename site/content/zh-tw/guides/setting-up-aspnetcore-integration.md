---
title: 設定 ASP.NET Core 整合
weight: 50
---

先安裝 `Davish.Result.AspNetCore.Http`（目標框架只有 `net10.0`）：

```bash
dotnet add package Davish.Result.AspNetCore.Http
```

## 註冊並接上 middleware

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.AddMinimalApiResult();   // 註冊 ToOk()/ToCreated()/... 的失敗處理 —— 只有完全不用它們時才能省略
});

var app = builder.Build();

app.UseExceptionHandler();   // 如果有用 MapExceptionToResult，這一行是必要的（見下方）
```

`AddResultAspNetCore` 會註冊把例外對應到 `Result` 的 dispatcher，並在內部呼叫
`services.AddProblemDetails()`。不帶參數的 `app.UseExceptionHandler()` 在啟動時需要這個 fallback。
`o.AddMinimalApiResult()` 則另外註冊預設的 `IMinimalApiFailureHandler`，以及讓
`MapExceptionToResult` 真正生效的例外處理器。

少了 `app.UseExceptionHandler()`，`MapExceptionToResult` 就不會生效。`AddResultAspNetCore`
只註冊 DI 服務，不會自動把它接進 middleware 管線。

## 每個部分各自負責什麼

| 呼叫 | 註冊了什麼 |
|---|---|
| `AddResultAspNetCore(o => ...)` | 例外對應 dispatcher、`services.AddProblemDetails()`。 |
| `o.AddMinimalApiResult(...)` | 預設的 `IMinimalApiFailureHandler`，以及 `MapExceptionToResult` 背後的例外處理器。 |
| `o.MapExceptionToResult<TException>(...)` | 一條把例外轉成 `Result` 的規則（見[把例外對應到 Result](mapping-exceptions-to-results.md)）。 |
| `o.ConfigureStatusCodes(...)` | `ErrorType` 對應到 HTTP 狀態碼的表（見[設定狀態碼](configuring-status-codes.md)）。 |
| `o.ConfigureProblemDetails(...)` | `AddProblemDetails()` 使用的 `ProblemDetailsOptions`。 |

## 延伸閱讀

- [把 Result 轉成 HTTP 回應](converting-results-to-http-responses.md)：`ToOk()`、
  `ToCreated()`、`ToAccepted()`、`ToNoContent()`。
- [自訂失敗處理邏輯](customizing-failure-handling.md)：替換預設的 `ProblemDetails`
  回應形狀。
- [把例外對應到 Result](mapping-exceptions-to-results.md)：處理拋出例外而非回傳
  `Result` 的程式碼。
- [設定狀態碼](configuring-status-codes.md)：註冊你自己的 `ErrorType` → 狀態碼對應。

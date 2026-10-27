---
title: 自訂失敗處理邏輯
weight: 70
---

預設的 `IMinimalApiFailureHandler` 會依[狀態碼對應](configuring-status-codes.md)產生
`ProblemDetails`；如果 `Error.Fields` 有內容，則產生 validation problem。

## 替換預設的處理器

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

只有在 `Result` 失敗（`Result.IsSuccess` 為 `false`）時，才會呼叫 `HandleAsync`。
`MinimalApiFailureHandler<THandler>()` 會替換掉 `IMinimalApiFailureHandler` 的 singleton 註冊，
之後 `ToOk()`/`ToCreated()` 等方法的失敗，以及透過 `MapExceptionToResult` 轉進來的例外，
都由這個處理器處理。

## 只想調整 ProblemDetails，不想整個換掉處理器

如果只想在預設的 `ProblemDetails`/`ValidationProblemDetails` 上加欄位或微調，設定
`ProblemDetailsOptions` 就好，不必自己寫處理器。

在 `AddResultAspNetCore` 裡呼叫 `o.ConfigureProblemDetails(...)`，設定的就是 ASP.NET Core
`[ApiController]`/`ControllerBase.Problem()` 使用的那份 `ProblemDetailsOptions`，
不只作用於 Minimal API：

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.ConfigureProblemDetails(p => p.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
});
```

## 相關文件

- [ASP.NET Core 整合模型](../concepts/aspnetcore-integration-model.md)：`MinimalApiResult.ExecuteAsync`
  在什麼時候解析失敗處理器。
- [Minimal API 結果型別](../reference/minimal-api-results.md)：`IFailureHandler<TResult>` /
  `IMinimalApiFailureHandler`。

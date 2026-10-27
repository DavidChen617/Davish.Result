---
title: 自訂失敗處理邏輯
weight: 70
---

預設的 `IMinimalApiFailureHandler` 會用 [狀態碼對應](configuring-status-codes.md) 產生一個
`ProblemDetails`（如果 `Error.Fields` 有內容，則是 validation problem）。

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

`HandleAsync` 只會在 `Result` 失敗時被呼叫（`Result.IsSuccess` 為 `false`）。它會替換掉
`IMinimalApiFailureHandler` 的單例註冊。之後 `ToOk()`/`ToCreated()` 等方法的失敗，
以及透過 `MapExceptionToResult` 轉進來的例外，都由同一個處理器處理。

## 只想調整 ProblemDetails，不想整個換掉處理器

如果你想保留預設的 `ProblemDetails`/`ValidationProblemDetails` 形狀，只是想加欄位或做些微調，可以設定 `ProblemDetailsOptions`，不用另外寫一個自訂處理器。`o.ConfigureProblemDetails(...)`
（跟其他設定同一個 `AddResultAspNetCore` 呼叫）設定的是 ASP.NET Core 的
`[ApiController]`/`ControllerBase.Problem()` 所使用的同一份 `ProblemDetailsOptions`，
跟 Minimal API 無關：

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.ConfigureProblemDetails(p => p.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
});
```

## 相關文件

- [ASP.NET Core 整合模型](../concepts/aspnetcore-integration-model.md)：失敗解析怎麼融入
  `MinimalApiResult.ExecuteAsync`。
- [Minimal API 結果型別](../reference/minimal-api-results.md)：`IFailureHandler<TResult>` /
  `IMinimalApiFailureHandler`。

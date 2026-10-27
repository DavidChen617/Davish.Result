---
title: ASP.NET Core 設定
weight: 40
---

命名空間：`Davish.Result`（套件 `Davish.Result.AspNetCore.Http`，目標框架僅有 `net10.0`）。

## `AddResultAspNetCore`

```csharp
public static IServiceCollection AddResultAspNetCore(
    this IServiceCollection services,
    Action<ResultAspNetCoreOptions> configureOptions);
```

註冊例外對應到 `Result` 的 dispatcher，並呼叫 `services.AddProblemDetails()`。例外對應要真正
生效，還需要 `app.UseExceptionHandler()` 以及 `o.AddMinimalApiResult()`。

## `ResultAspNetCoreOptions`

傳入 `AddResultAspNetCore` 設定委派的參數。

```csharp
public sealed class ResultAspNetCoreOptions
{
    public void MapExceptionToResult<TException>(Func<HttpContext, TException, Result> mapper)
        where TException : Exception;

    public void MapExceptionToResult<TException>(
        Func<HttpContext, TException, CancellationToken, ValueTask<Result>> mapper)
        where TException : Exception;

    public void MapExceptionToResult<TException, TMapper>()
        where TException : Exception
        where TMapper : class, IExceptionMapper<TException>;

    public void AddMinimalApiResult(Action<MinimalApiResultOptions>? configureOptions = null);

    public void ConfigureStatusCodes(Action<ResultHttpOptionsBuilder> configure);

    public void ConfigureProblemDetails(Action<ProblemDetailsOptions> configure);
}
```

- `MapExceptionToResult<TException>(...)`：有同步與非同步兩個多載，為 `TException`（以及它的
  子類別；如果沿同一條繼承鏈註冊了多個規則，最具體的那個會勝出）註冊一條轉換規則。
- `MapExceptionToResult<TException, TMapper>()`：DI 解析版本。`TMapper` 用
  `TryAddTransient` 註冊，並且在每次發生例外時從 `HttpContext.RequestServices` 解析出來，
  不是在註冊當下就解析（避免 `TMapper` 依賴 scoped 服務時變成 captive dependency）。
- `AddMinimalApiResult(configureOptions)`：註冊預設的 `IMinimalApiFailureHandler`
  （`TryAddSingleton`，所以先註冊的會生效）以及負責分派 `MapExceptionToResult` 規則的例外
  處理器；`configureOptions` 是選填的。
- `ConfigureStatusCodes(...)`：替換 process-wide 的狀態碼對應表（參考
  [狀態碼對應](status-code-mapping.md)）。立即生效且不會鎖定，所以可以再次呼叫（例如每個
  測試 host 各呼叫一次）。
- `ConfigureProblemDetails(...)`：設定 `AddResultAspNetCore` 已經呼叫的
  `services.AddProblemDetails()` 所使用的 `ProblemDetailsOptions`。這與 host 無關，
  MVC 的 `[ApiController]`/`ControllerBase.Problem()` 用的是同一份設定。

## `MinimalApiResultOptions`

傳入 `AddMinimalApiResult` 選填設定委派的參數。

```csharp
public sealed class MinimalApiResultOptions
{
    public void MinimalApiFailureHandler<THandler>() where THandler : class, IMinimalApiFailureHandler;
}
```

用 `THandler` 替換掉已註冊的 `IMinimalApiFailureHandler` 單例（`services.Replace(...)`）。
參考 [自訂失敗處理邏輯](../guides/customizing-failure-handling.md)。

## `IExceptionMapper<TSource>`

```csharp
public interface IExceptionMapper<in TSource> where TSource : Exception
{
    ValueTask<Result> MapAsync(HttpContext context, TSource source, CancellationToken cancellationToken);
}
```

如果 `MapExceptionToResult<TException, TMapper>()` 的註冊需要從 DI 解析服務，就實作這個
介面。

## 範例

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.MapExceptionToResult<BookingLockedException>((context, exception) =>
        Result.Failure(new Error("Booking.Locked", "This booking is already confirmed.", ErrorType.Conflict)));

    o.AddMinimalApiResult();
});

var app = builder.Build();

app.UseExceptionHandler(); // MapExceptionToResult 生效的必要條件
```

## 相關文件

- [設定 ASP.NET Core 整合](../guides/setting-up-aspnetcore-integration.md)
- [把例外對應到 Result](../guides/mapping-exceptions-to-results.md)
- [ASP.NET Core 整合模型](../concepts/aspnetcore-integration-model.md)

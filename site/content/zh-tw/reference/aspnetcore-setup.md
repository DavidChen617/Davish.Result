---
title: ASP.NET Core 設定
weight: 40
---

命名空間：`Davish.Result`（套件 `Davish.Result.AspNetCore.Http`，目標框架只有 `net10.0`）。

## `AddResultAspNetCore`

```csharp
public static IServiceCollection AddResultAspNetCore(
    this IServiceCollection services,
    Action<ResultAspNetCoreOptions> configureOptions);
```

註冊把例外對應到 `Result` 的 dispatcher，並呼叫 `services.AddProblemDetails()`。例外對應要真正
生效，還需要 `app.UseExceptionHandler()` 和 `o.AddMinimalApiResult()`。

## `ResultAspNetCoreOptions`

`AddResultAspNetCore` 設定委派收到的參數。

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

- `MapExceptionToResult<TException>(...)`：為 `TException` 及其子類別註冊一條轉換規則，有同步和
  非同步兩個多載。同一條繼承鏈上註冊了多條規則時，最具體的那條優先。
- `MapExceptionToResult<TException, TMapper>()`：從 DI 解析的版本。`TMapper` 以
  `TryAddTransient` 註冊，每次發生例外時才從 `HttpContext.RequestServices` 解析，不是在註冊時
  解析。這樣 `TMapper` 依賴 scoped 服務時，才不會變成 captive dependency。
- `AddMinimalApiResult(configureOptions)`：註冊預設的 `IMinimalApiFailureHandler`，以及分派
  `MapExceptionToResult` 規則的例外處理器。前者用 `TryAddSingleton` 註冊，所以如果之前已經註冊過
  處理器，會保留先前的註冊。`configureOptions` 為選填。
- `ConfigureStatusCodes(...)`：替換 process-wide 的狀態碼對應表（參考
  [狀態碼對應](status-code-mapping.md)）。立即生效、不加鎖，所以可以重複呼叫，例如每個
  測試 host 各呼叫一次。
- `ConfigureProblemDetails(...)`：設定 `services.AddProblemDetails()` 使用的
  `ProblemDetailsOptions`（`AddResultAspNetCore` 已經呼叫過 `AddProblemDetails()`）。這份設定
  不限於 Minimal API，MVC 的 `[ApiController]`/`ControllerBase.Problem()` 用的也是它。

## `MinimalApiResultOptions`

`AddMinimalApiResult` 選填設定委派收到的參數。

```csharp
public sealed class MinimalApiResultOptions
{
    public void MinimalApiFailureHandler<THandler>() where THandler : class, IMinimalApiFailureHandler;
}
```

用 `THandler` 替換已註冊的 `IMinimalApiFailureHandler` singleton（`services.Replace(...)`）。
參考[自訂失敗處理邏輯](../guides/customizing-failure-handling.md)。

## `IExceptionMapper<TSource>`

```csharp
public interface IExceptionMapper<in TSource> where TSource : Exception
{
    ValueTask<Result> MapAsync(HttpContext context, TSource source, CancellationToken cancellationToken);
}
```

用 `MapExceptionToResult<TException, TMapper>()` 註冊、需要從 DI 取得服務的規則，要實作這個
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

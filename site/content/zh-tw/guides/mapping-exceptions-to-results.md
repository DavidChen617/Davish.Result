---
title: 把例外對應到 Result
weight: 80
---

有些程式碼會拋出例外，而不是回傳 `Result`，例如第三方函式庫。`MapExceptionToResult` 可以把
這類例外轉成 `Result`，交給跟 `ToOk()` 相同的失敗處理流程。

## 用委派註冊一條對應規則

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.MapExceptionToResult<BookingLockedException>((context, exception) =>
        Result.Failure(new Error("Booking.Locked", "This booking is already confirmed.", ErrorType.Conflict)));

    o.AddMinimalApiResult();
});
```

同步委派（`Func<HttpContext, TException, Result>`）和非同步委派
（`Func<HttpContext, TException, CancellationToken, ValueTask<Result>>`）都有提供。
規則需要做非同步工作（例如查資料庫）時，就用非同步版本。

少了 `app.UseExceptionHandler()`，這個機制就不會生效。`AddResultAspNetCore` 只註冊
DI 服務，不會自動把它接進 middleware 管線。

## 依繼承關係比對，最具體的優先

為基底例外型別註冊的規則，也會套用到它的子類別。如果基底型別和衍生型別都註冊了規則，
拋出的是衍生型別時，會優先套用衍生型別的規則。

## 註冊需要依賴注入的規則

規則需要從 DI 取得服務時，改用 `MapExceptionToResult<TException, TMapper>()`，其中
`TMapper : IExceptionMapper<TException>`：

```csharp
public sealed class BookingLockedExceptionMapper : IExceptionMapper<BookingLockedException>
{
    public ValueTask<Result> MapAsync(HttpContext context, BookingLockedException source, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Result.Failure(new Error("Booking.Locked", "This booking is already confirmed.", ErrorType.Conflict)));
}

builder.Services.AddResultAspNetCore(o =>
{
    o.MapExceptionToResult<BookingLockedException, BookingLockedExceptionMapper>();
});
```

`TMapper` 以 transient 註冊。每次發生例外時，都從目前請求的 `HttpContext.RequestServices`
解析，而不是在註冊時解析一次就一直沿用，所以可以放心依賴 scoped 服務，不會變成
captive dependency。

## 相關文件

- [ASP.NET Core 整合模型](../concepts/aspnetcore-integration-model.md)：例外對應和
  `Result` → `IResult` 轉換，怎麼交給同一個失敗處理器。
- [Minimal API 結果型別](../reference/minimal-api-results.md)：`IExceptionMapper<TSource>`。

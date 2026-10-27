---
title: 把例外對應到 Result
weight: 80
---

對於那些拋出例外、而不是回傳 `Result` 的程式碼（例如某個第三方函式庫），
`MapExceptionToResult` 可以把捕捉到的例外轉換進跟 `ToOk()` 一樣的失敗處理管線。

## 用委派註冊一條對應規則

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.MapExceptionToResult<BookingLockedException>((context, exception) =>
        Result.Failure(new Error("Booking.Locked", "This booking is already confirmed.", ErrorType.Conflict)));

    o.AddMinimalApiResult();
});
```

同步委派（`Func<HttpContext, TException, Result>`）跟非同步委派
（`Func<HttpContext, TException, CancellationToken, ValueTask<Result>>`）都有提供，依對應規則是否需要非同步工作（例如查詢資料庫）選擇。

`app.UseExceptionHandler()` 是這個機制生效的必要條件。`AddResultAspNetCore` 只註冊
DI 服務，不會自動把它接進 middleware 管線。

## 比對是依繼承關係，最具體的優先

為一個基底例外型別註冊的規則，也會比對它的子類別。如果同時為一個基底型別跟一個衍生型別都
註冊了規則，衍生型別的規則會在該衍生型別的實例上優先套用。

## 註冊一條需要依賴注入的對應規則

`MapExceptionToResult<TException, TMapper>()`（其中 `TMapper : IExceptionMapper<TException>`）
提供給需要從 DI 解析服務的對應規則使用：

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

`TMapper` 會以 transient 的方式註冊，並且在每次發生例外時，從目前請求的
`HttpContext.RequestServices` 解析出來，而不是在註冊當下解析一次並捕捉，所以可以安全地
依賴 scoped 服務，不會變成 captive dependency。

## 相關文件

- [ASP.NET Core 整合模型](../concepts/aspnetcore-integration-model.md)：例外對應跟
  `Result` → `IResult` 轉換怎麼匯流到同一個失敗處理器。
- [Minimal API 結果型別](../reference/minimal-api-results.md)：`IExceptionMapper<TSource>`。

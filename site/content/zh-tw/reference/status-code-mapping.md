---
title: 狀態碼對應
weight: 60
---

命名空間：`Davish.Result`（套件 `Davish.Result.AspNetCore.Http`）。

## `ResultHttpOptions`

```csharp
public static class ResultHttpOptions
{
    // internal: static void Configure(Action<ResultHttpOptionsBuilder> configure)
    // internal: static int ResolveStatusCode(ErrorType errorType)
}
```

`ResultHttpOptions` 是公開的靜態類別，但它的成員都是 `internal`，請透過
`ResultAspNetCoreOptions.ConfigureStatusCodes(...)` 來設定對應表（參考
[ASP.NET Core 設定](aspnetcore-setup.md)）。

- `ConfigureStatusCodes` 會整份替換這個對應表，並且立刻套用。沒有任何鎖，再次呼叫（即使在
  狀態碼已被解析過之後）也只是直接替換對應表。
- 內建預設值（當 `ResultHttpOptionsBuilder.UseDefault` 為 `true`，也就是預設值時會被種入）：

  | `ErrorType` | 狀態碼 |
  |---|---|
  | `Validation` | 400 |
  | `NullValue` | 400 |
  | `BadRequest` | 400 |
  | `NotFound` | 404 |
  | `Unauthorized` | 401 |
  | `Forbidden` | 403 |
  | `Conflict` | 409 |
  | `TooManyRequests` | 429 |
  | `Unexpected` | 500 |
  | `ServiceUnavailable` | 503 |

- 狀態碼是透過 `ErrorType.ToStatusCode()`（見下方）在內部解析的；沒有登記的 `ErrorType`
  會解析成 `500`。
- 這是 process-wide 的靜態狀態（每次設定時會原子性地換掉一個
  `ConcurrentDictionary<ErrorType, int>`），不是每個請求各自從 DI 解析。參考
  [錯誤類型對應狀態碼](../concepts/status-code-mapping.md)。

## `ResultHttpOptionsBuilder`

```csharp
public sealed class ResultHttpOptionsBuilder
{
    public bool UseDefault { get; set; } = true;
    public IReadOnlyDictionary<ErrorType, int>? CustomMap { get; set; }
}
```

`CustomMap` 會在內建預設值套用之後（如果 `UseDefault` 為 `true`）才套用，並覆蓋掉共用同一個
`ErrorType` 的項目。

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.ConfigureStatusCodes(v =>
    {
        v.CustomMap = new Dictionary<ErrorType, int>
        {
            [OrderErrorType.OutOfStock] = StatusCodes.Status409Conflict,
        };
    });
});
```

## `ErrorTypeHttpExtension`

```csharp
public static class ErrorTypeHttpExtension
{
    public static int ToStatusCode(this ErrorType errorType);
}
```

透過 `ConfigureStatusCodes` 設定的對應表，解析出 `errorType` 對應的 HTTP 狀態碼
（沒有登記的話回傳 500）。這個方法是公開的，讓消費端在寫自己的 `ToXxx()` Minimal API
結果擴充方法時，可以重用已經設定好的對應表。

## 相關文件

- [設定狀態碼](../guides/configuring-status-codes.md)
- [錯誤類型對應狀態碼](../concepts/status-code-mapping.md)

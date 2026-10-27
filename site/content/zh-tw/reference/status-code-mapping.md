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

`ResultHttpOptions` 是公開的靜態類別，但成員都是 `internal`。設定對應表請用
`ResultAspNetCoreOptions.ConfigureStatusCodes(...)`（參考
[ASP.NET Core 設定](aspnetcore-setup.md)）。

- `ConfigureStatusCodes` 會整份替換對應表，並立刻套用。過程不加鎖；即使已經解析過狀態碼，
  再次呼叫也只是直接換掉對應表。
- 內建預設值（`ResultHttpOptionsBuilder.UseDefault` 為 `true` 時會先放進對應表，`true` 也是預設值）：

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

- 狀態碼由 `ErrorType.ToStatusCode()`（見下方）解析；沒有登記的 `ErrorType` 會得到 `500`。
- 這是 process-wide 的靜態狀態，不是每個請求各自從 DI 解析。每次設定時，會以原子操作換掉整個
  `ConcurrentDictionary<ErrorType, int>`。參考
  [錯誤類型對應狀態碼](../concepts/status-code-mapping.md)。

## `ResultHttpOptionsBuilder`

```csharp
public sealed class ResultHttpOptionsBuilder
{
    public bool UseDefault { get; set; } = true;
    public IReadOnlyDictionary<ErrorType, int>? CustomMap { get; set; }
}
```

`CustomMap` 在內建預設值之後套用（如果 `UseDefault` 為 `true`），遇到相同的 `ErrorType` 時，
會覆蓋預設值。

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

依 `ConfigureStatusCodes` 設定的對應表，查出 `errorType` 對應的 HTTP 狀態碼，沒有登記時回傳 500。
這個方法公開出來，是為了讓你自己寫 `ToXxx()` Minimal API 擴充方法時，也能沿用設定好的對應表。

## 相關文件

- [設定狀態碼](../guides/configuring-status-codes.md)
- [錯誤類型對應狀態碼](../concepts/status-code-mapping.md)

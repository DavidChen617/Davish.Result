---
title: 設定狀態碼
weight: 90
---

失敗會依 `Error.Type` 對應到一個狀態碼。內建的分類會照直覺對應（`Validation`/`NullValue`/`BadRequest`
→ 400、`NotFound` → 404、`Unauthorized` → 401、`Forbidden` → 403、`Conflict` → 409、
`TooManyRequests` → 429、`ServiceUnavailable` → 503、`Unexpected` 以及任何未登記的類型 → 500）。

## 註冊你自己的分類

跟其他設定放在同一個 `AddResultAspNetCore` 呼叫裡，透過 `o.ConfigureStatusCodes(...)`：

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.ConfigureStatusCodes(v =>
    {
        // v.UseDefault = false;   // 不使用上面的內建對應

        v.CustomMap = new Dictionary<ErrorType, int>
        {
            [OrderErrorType.OutOfStock] = StatusCodes.Status409Conflict,
            [OrderErrorType.PaymentDeclined] = StatusCodes.Status402PaymentRequired,
        };
    });
});
```

`ResultHttpOptions.Configure` 是 `internal`，所以 `ConfigureStatusCodes` 是唯一的入口。
這是 process-wide 的靜態設定，會立刻套用，不是每個請求各自從 DI 容器解析，概念上跟設定
`JsonSerializerOptions` 或 Dapper 的 `SqlMapper.Settings` 一樣。因為 `ErrorType` 是依值比較，
`OrderErrorType.OutOfStock` 跟任何你自己建立、名字相同的 `ErrorType` 都會對應到同一筆。

## 再次呼叫會替換對應表

這裡沒有鎖。再次呼叫 `ConfigureStatusCodes` 會整份替換對應表，即使已經為真實請求解析過狀態碼。這對在同一個 process 裡啟動多個 host 的測試很方便（每個 host 的
`AddResultAspNetCore` 只是重新套用它的對應表）。反過來說：如果同一個 process 裡兩個 host 同時
想要*不同*的對應表，最後設定的那個會對兩邊都生效。

## 相關文件

- [錯誤類型對應狀態碼](../concepts/status-code-mapping.md)：為什麼這是靜態的、process-wide
  的設定，而不是走 DI 解析出來的選項。
- [狀態碼對應](../reference/status-code-mapping.md)：`ResultHttpOptions`、
  `ResultHttpOptionsBuilder`、`ErrorTypeHttpExtension.ToStatusCode()`。

---
title: 設定狀態碼
weight: 90
---

失敗結果依 `Error.Type` 決定狀態碼。內建分類的對應大致符合直覺：`Validation`/`NullValue`/`BadRequest`
→ 400、`NotFound` → 404、`Unauthorized` → 401、`Forbidden` → 403、`Conflict` → 409、
`TooManyRequests` → 429、`ServiceUnavailable` → 503，`Unexpected` 和所有沒登記的類型 → 500。

## 註冊你自己的分類

在 `AddResultAspNetCore` 裡，跟其他設定一起呼叫 `o.ConfigureStatusCodes(...)`：

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
這是 process-wide 的靜態設定（整個 process 共用一份），會立刻套用，不是每個請求各自從 DI 容器解析，
概念跟 Dapper 的 `SqlMapper.Settings` 一樣。`ErrorType` 依值比較，所以你自己建立的 `ErrorType`
只要名字跟 `OrderErrorType.OutOfStock` 相同，就會對應到同一筆。

## 再次呼叫會替換對應表

再次呼叫 `ConfigureStatusCodes` 會整份替換對應表，即使已經有請求用過舊的表也一樣，過程中沒有加鎖。

在同一個 process 裡啟動多個 host 的測試會因此很方便：每個 host 的 `AddResultAspNetCore`
只是重新套用自己的對應表。反過來說，如果同一個 process 裡兩個 host 同時想要*不同*的對應表，
最後設定的那份會對兩邊都生效。

## 相關文件

- [錯誤類型對應狀態碼](../concepts/status-code-mapping.md)：為什麼這是 process-wide 的靜態設定，
  而不是透過 DI 解析的選項。
- [狀態碼對應](../reference/status-code-mapping.md)：`ResultHttpOptions`、
  `ResultHttpOptionsBuilder`、`ErrorTypeHttpExtension.ToStatusCode()`。

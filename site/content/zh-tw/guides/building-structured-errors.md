---
title: 建立結構化錯誤
weight: 30
---

## 欄位層級（驗證型）錯誤

`Error.Fields` 用欄位名稱當 key，儲存對應的訊息。用 `AddFieldError` 以流暢語法建立它：

```csharp
var error = new Error("Validation", "One or more fields are invalid")
    .AddFieldError("Email", "Email is required")
    .AddFieldError("Password", new[] { "Too short", "Must contain a digit" });

// error.Fields["Password"] => ["Too short", "Must contain a digit"]
```

對同一個欄位名稱再次呼叫 `AddFieldError`，會把訊息附加到既有的清單後面，而不是取代它。
`AddFieldError` 不會修改被呼叫的 `Error`，而是回傳一個加上新訊息的新 `Error`。對共享實例
（例如 `Error.NullValue`）呼叫時，共享的單例不受影響，必須使用回傳值。

## Metadata

`WithMetadata` 可以在錯誤上附加自由格式的 key/value 資料，用來放不適合寫進
`Code`/`Description` 的上下文，例如實體 id 或重試提示：

```csharp
var error = new Error("Rate.Limited", "Too many requests.", ErrorType.TooManyRequests)
    .WithMetadata("retryAfterSeconds", 30);

error.Metadata["retryAfterSeconds"];   // 30
```

跟 `AddFieldError` 一樣，它回傳新的 `Error`，不修改原本的實例。對同一個 key 再次設值會覆蓋舊值。Metadata 會參與相等性比較，但預設的失敗處理器不會把它寫進 HTTP 回應。

## 因果鏈

`CausedBy` 用來附加一個或多個底層錯誤，概念上跟 `Exception.InnerException`（多個原因時則是
`AggregateException.InnerExceptions`）相同：

```csharp
var dbError = new Error("Db.Timeout", "The database connection timed out.");
var error = new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected)
    .CausedBy(dbError);

error.InnerError;      // dbError —— 第一個原因（如果沒有原因則為 null）
error.GetRootCause();   // 沿著單一原因的鏈往下走到最深的錯誤；
                        // 遇到零個或多個原因（聚合點）就停下來
```

`CausedBy` 同樣回傳新的 `Error`，不修改原本的實例。多次呼叫會依加入順序累積多個原因。

## 自訂錯誤類型

用跟內建分類一樣的方式，把應用程式專屬的分類宣告成 `static readonly ErrorType` 值：

```csharp
public static class OrderErrorType
{
    public static readonly ErrorType OutOfStock = new(nameof(OutOfStock));
    public static readonly ErrorType PaymentDeclined = new(nameof(PaymentDeclined));
}

var error = new Error("Order.OutOfStock", "Item is out of stock", OrderErrorType.OutOfStock);
```

`ErrorType` 是 `record struct`，所以是依名稱比較：`new ErrorType("NotFound")` 會等於
`ErrorType.NotFound`，與建立方式無關。選名稱時要避免跟內建分類撞名，除非你有意如此。想把自訂分類對應到 HTTP 狀態碼，參考
[設定狀態碼](configuring-status-codes.md)。

## 相關文件

- [Result 與 Error 模型](../concepts/result-and-error-model.md)：為什麼 `Error` 是不可變的，
  以及相等性是怎麼運作的。
- [Error、ErrorType 與 ExceptionalError](../reference/error.md)：完整 API。

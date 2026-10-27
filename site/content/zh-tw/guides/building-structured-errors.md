---
title: 建立結構化錯誤
weight: 30
---

## 欄位層級（驗證型）錯誤

`Error.Fields` 以欄位名稱為 key，存放各欄位的錯誤訊息。可以用 `AddFieldError` 串接呼叫來建立：

```csharp
var error = new Error("Validation", "One or more fields are invalid")
    .AddFieldError("Email", "Email is required")
    .AddFieldError("Password", new[] { "Too short", "Must contain a digit" });

// error.Fields["Password"] => ["Too short", "Must contain a digit"]
```

對同一個欄位再次呼叫 `AddFieldError`，訊息會接在原本的清單後面，不會取代。
`AddFieldError` 不修改原本的 `Error`，而是回傳加上新訊息的新 `Error`。對共享單例
（例如 `Error.NullValue`）呼叫時，單例本身不受影響，你必須使用回傳值。

## Metadata

`WithMetadata` 可以在錯誤上附加任意的 key/value 資料，存放不適合寫進
`Code`/`Description` 的資訊，例如實體 id 或重試提示：

```csharp
var error = new Error("Rate.Limited", "Too many requests.", ErrorType.TooManyRequests)
    .WithMetadata("retryAfterSeconds", 30);

error.Metadata["retryAfterSeconds"];   // 30
```

跟 `AddFieldError` 一樣，它回傳新的 `Error`，不修改原本的實例。同一個 key 再設一次值，會覆蓋舊值。
Metadata 會納入相等性比較，但預設的失敗處理器不會把它寫進 HTTP 回應。

## 因果鏈

`CausedBy` 用來附加一個或多個底層錯誤，概念跟 `Exception.InnerException` 相同；
有多個原因時，則對應 `AggregateException.InnerExceptions`：

```csharp
var dbError = new Error("Db.Timeout", "The database connection timed out.");
var error = new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected)
    .CausedBy(dbError);

error.InnerError;      // dbError —— 第一個原因（如果沒有原因則為 null）
error.GetRootCause();   // 沿著單一原因的鏈往下走到最深的錯誤；
                        // 遇到零個或多個原因（聚合點）就停下來
```

`CausedBy` 同樣回傳新的 `Error`，不修改原本的實例。多次呼叫時，原因會依加入順序累積。

## 自訂錯誤類型

比照內建分類的寫法，把應用程式專屬的分類宣告成 `static readonly ErrorType`：

```csharp
public static class OrderErrorType
{
    public static readonly ErrorType OutOfStock = new(nameof(OutOfStock));
    public static readonly ErrorType PaymentDeclined = new(nameof(PaymentDeclined));
}

var error = new Error("Order.OutOfStock", "Item is out of stock", OrderErrorType.OutOfStock);
```

`ErrorType` 是 `record struct`，依名稱比較：`new ErrorType("NotFound")` 等於
`ErrorType.NotFound`，跟怎麼建立的無關。因此取名時要避開內建分類的名字，除非你就是想讓它們相等。

想把自訂分類對應到 HTTP 狀態碼，參考[設定狀態碼](configuring-status-codes.md)。

## 相關文件

- [Result 與 Error 模型](../concepts/result-and-error-model.md)：`Error` 為什麼不可變，
  以及相等性怎麼比較。
- [Error、ErrorType 與 ExceptionalError](../reference/error.md)：完整 API。

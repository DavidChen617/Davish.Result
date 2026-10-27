---
title: Result 與 Error 模型
weight: 10
---

## 成功與失敗在結構上互斥

`Result.IsSuccess` 是計算出來的，不是儲存的欄位：它等於 `Error == Error.None`。`Result` 只能透過
`Result.Success()`/`Result.Success<TValue>(value)` 或
`Result.Failure(error)`/`Result.Failure<TValue>(error)` 來建立，兩種建構方式都會強制這個不變條件：

- `Result.Failure(error)` 在 `error == Error.None` 時會拋出 `InvalidResultStateException`
  （`Error.None` 描述的是成功，所以它不能同時用來描述失敗）。
- `Result<TValue>` 的建構子在 `error == Error.None` 且 `value` 為 `null` 時會拋出
  `InvalidResultStateException`（成功必須帶著一個值）。

沒有任何公開的建構子可以直接建立不一致的 `Result`，靜態工廠方法是唯一的進入點。

## `Result<TValue>` 要求 `TValue : notnull`

`Result.Success<TValue>` 跟 `Result.Failure<TValue>` 都限制 `TValue : notnull`。`null`
值不會產生帶著 `null` 的成功 `Result<TValue>`。從值轉換過來的隱含轉換
（`public static implicit operator Result<TValue>(TValue? value)`）會把 `null` 轉成帶著
`Error.NullValue` 的失敗結果：

```csharp
string? value = null;
Result<string> result = value;   // 失敗結果，result.Error == Error.NullValue
```

## 錯誤是不可變的，每次都衍生出新的實例

`Error` 是一個 `record`，它的 `AddFieldError`/`CausedBy` API 從不修改呼叫端本身，每次呼叫都回傳
一個套用了新增內容的新 `Error`。這對兩個標準單例 `Error.None` 和 `Error.NullValue` 尤其重要：
呼叫 `Error.NullValue.AddFieldError(...)` 不會污染這個共享單例，你必須使用回傳值。

`Error` 的相等性是結構化的（record equality），比較 `Code`、`Description`、`Type`、`Fields`，
以及 `Causes`（`Causes` 跟 `Fields` 裡的訊息清單都對順序敏感）。兩個各自獨立建立、但內容相同的
`Error` 實例會被視為相等。

## `ErrorType` 是一個值，不是列舉

`ErrorType` 是一個包著 `string Value` 的 `readonly record struct`。內建的分類
（`ErrorType.Validation`、`ErrorType.NotFound` 等）都是 `static readonly` 欄位。宣告自己的分類
只要再宣告一個名字不同的 `static readonly ErrorType`。因為它是 record struct，兩個名字相同的
`ErrorType` 值會相等，不管是從哪個欄位（或哪次 `new ErrorType(...)` 呼叫）產生的：

```csharp
var notFoundByValue = new ErrorType(ErrorType.NotFound.Value);
notFoundByValue == ErrorType.NotFound;   // true
```

用 `null` 或空字串建立 `ErrorType` 會拋出 `InvalidErrorTypeException`。

## 因果鏈仿照 `Exception.InnerException` 的設計

`Error.Causes` 是一個有序、不可變的清單；`Error.InnerError` 就是 `Causes[0]`（如果是空的則為
`null`），關係就像 `Exception.InnerException` 對 `AggregateException.InnerExceptions`。
`GetRootCause()` 會沿著只有單一原因的鏈一路往下走，直到某個節點沒有原因（葉節點）或有超過一個原因
（一個聚合點，仿照 `AggregateException.GetBaseException`）才停下來。

## `ExceptionalError` 把捕捉到的例外橋接進這個模型

`ExceptionalError : Error` 包裝一個捕捉到的 `Exception`：`Code` 是例外的型別名稱，`Description` 是
`Exception.Message`，`Type` 則是 `ErrorType.Unexpected`。`Exception.InnerException` 會被遞迴包裝，
再透過 `CausedBy` 附加上去，所以在結果上呼叫 `Error.GetRootCause()` 走的鏈路，跟
`Exception.InnerException` 會走的鏈路是一致的。

`Result.Failure(exception)` 直接用 `ExceptionalError.From(exception)`，原始的
`Exception.Message` 會變成 `Error.Description`。`Result.Failure(exception, error)` 則改用
`ExceptionalError.From(exception, error)`，把例外包裝成你提供的、對客戶端安全的 `error` 的
`InnerError`，讓原始例外訊息不會出現在 `Description` 裡。什麼時候該用哪一個，參考
[包裝捕捉到的例外](../guides/wrapping-exceptions.md)。

## 這個套件自己會拋出的例外

這個函式庫直接拋出的所有例外都衍生自抽象類別 `ResultException : InvalidOperationException`，
所以 `catch (ResultException)` 可以處理其中任何一個：

- `InvalidResultStateException` — 建立了一個不一致的成功/錯誤組合。
- `ResultValueUnavailableException` — 在失敗的結果上讀取了 `Result<TValue>.Value`。
- `InvalidErrorTypeException` — 用 null/空字串建立了 `ErrorType`。

完整清單參考
[例外](../reference/exceptions.md)。

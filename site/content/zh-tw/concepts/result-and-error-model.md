---
title: Result 與 Error 模型
weight: 10
---

## 成功與失敗在結構上互斥

`Result.IsSuccess` 不是儲存的欄位，而是算出來的：它等於 `Error == Error.None`。建立 `Result` 要透過
`Result.Success()`/`Result.Success<TValue>(value)` 或
`Result.Failure(error)`/`Result.Failure<TValue>(error)`，這兩組方法都會檢查狀態是否一致：

- `Result.Failure(error)` 在 `error == Error.None` 時會拋出 `InvalidResultStateException`。
  `Error.None` 代表成功，不能拿來描述失敗。
- `Result<TValue>` 的建構子在 `error == Error.None` 且 `value` 為 `null` 時會拋出
  `InvalidResultStateException`，因為成功必須帶有值。

`Result` 沒有公開的建構子（只有給衍生類別用的 `protected` 建構子），外部程式碼只能透過靜態工廠方法，
或是會轉呼叫這些方法的隱含轉換來建立。

## `Result<TValue>` 要求 `TValue : notnull`

`Result.Success<TValue>` 跟 `Result.Failure<TValue>` 都限制 `TValue : notnull`，所以不會出現
值為 `null` 的成功 `Result<TValue>`。從值轉換的隱含轉換
（`public static implicit operator Result<TValue>(TValue? value)`）遇到 `null` 時，會轉成帶有
`Error.NullValue` 的失敗結果：

```csharp
string? value = null;
Result<string> result = value;   // 失敗結果，result.Error == Error.NullValue
```

## 錯誤不可變，每次修改都產生新實例

`Error` 是 `record`。`AddFieldError`、`WithMetadata`、`CausedBy` 都不會修改原本的 `Error`，
而是回傳加上新內容的新 `Error`。這對兩個共享單例 `Error.None` 和 `Error.NullValue` 特別重要：
呼叫 `Error.NullValue.AddFieldError(...)` 不會污染單例本身，你必須使用回傳值。

`Error` 依內容比較是否相等（record equality），比較的欄位有 `Code`、`Description`、`Type`、`Fields`、
`Metadata` 和 `Causes`。其中 `Causes` 與 `Fields` 裡的訊息清單會比較順序。兩個分別建立、內容相同的
`Error` 視為相等。

## `ErrorType` 是值，不是列舉

`ErrorType` 是包著 `string Value` 的 `readonly record struct`。內建分類
（`ErrorType.Validation`、`ErrorType.NotFound` 等）都是 `static readonly` 欄位；要自訂分類，
再宣告一個不同名字的 `static readonly ErrorType` 就好。因為它是 record struct，只要名字相同，
兩個 `ErrorType` 就相等，不管它來自哪個欄位，或是哪一次 `new ErrorType(...)`：

```csharp
var notFoundByValue = new ErrorType(ErrorType.NotFound.Value);
notFoundByValue == ErrorType.NotFound;   // true
```

用 `null` 或空字串建立 `ErrorType` 會拋出 `InvalidErrorTypeException`。

## 因果鏈仿照 `Exception.InnerException`

`Error.Causes` 是有順序、不可變的清單。`Error.InnerError` 就是 `Causes[0]`，清單為空時是 `null`。
兩者的關係，就像 `Exception.InnerException` 和 `AggregateException.InnerExceptions`。

`GetRootCause()` 會沿著單一原因一路往下找，遇到下列兩種節點就停下：沒有原因的葉節點，
或是有多個原因的聚合點。後者的行為仿照 `AggregateException.GetBaseException`。

## `ExceptionalError` 把捕捉到的例外接進這個模型

`ExceptionalError : Error` 包裝捕捉到的 `Exception`：`Code` 是例外的型別名稱，`Description` 是
`Exception.Message`，`Type` 是 `ErrorType.Unexpected`。`Exception.InnerException` 會遞迴包裝成
`ExceptionalError`，再用 `CausedBy` 掛上去。因此，對結果呼叫 `Error.GetRootCause()` 走過的路徑，
跟沿著 `Exception.InnerException` 往下走的路徑一致。

`Result.Failure(exception)` 直接使用 `ExceptionalError.From(exception)`，原始的
`Exception.Message` 會成為 `Error.Description`。`Result.Failure(exception, error)` 則使用
`ExceptionalError.From(exception, error)`：對外呈現的是你提供的 `error`，例外只掛在它底下，
成為 `InnerError`，原始例外訊息就不會出現在 `Description` 裡。兩者怎麼選，參考
[包裝捕捉到的例外](../guides/wrapping-exceptions.md)。

## 套件本身會拋出的例外

這個函式庫直接拋出的例外，都衍生自抽象類別 `ResultException : InvalidOperationException`，
所以 `catch (ResultException)` 都接得住：

- `InvalidResultStateException`：建立了不一致的成功/錯誤組合。
- `ResultValueUnavailableException`：在失敗的結果上讀取 `Result<TValue>.Value`。
- `InvalidErrorTypeException`：用 `null` 或空字串建立 `ErrorType`。

完整清單參考[例外](../reference/exceptions.md)。

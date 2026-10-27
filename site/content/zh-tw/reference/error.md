---
title: Error、ErrorType 與 ExceptionalError
weight: 20
---

命名空間：`Davish.Result`（套件 `Davish.Result`）。

## `Error`

描述錯誤內容的不可變 `record`。

```csharp
public record Error
{
    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }                                              // 預設為 ErrorType.Validation
    public IReadOnlyList<Error> Causes { get; }                                 // 預設為 []
    public Error? InnerError { get; }                                           // Causes[0]，或 null
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Fields { get; }   // 預設為 {}
    public IReadOnlyDictionary<string, object?> Metadata { get; }               // 預設為 {}

    public Error(string code, string description);
    public Error(string code, string description, ErrorType type);
    public Error(string code, string description, Dictionary<string, List<string>> fields);   // fields 為 null 時拋出 ArgumentNullException

    public static Error None { get; }        // "" / "" / ErrorType.None —— 給成功結果使用
    public static Error NullValue { get; }   // "Null.Value" / "Null value was provided" / ErrorType.NullValue

    public static implicit operator Result(Error error);

    public Error AddFieldError(string field, string message);
    public Error AddFieldError(string field, ICollection<string> messages);

    public Error WithMetadata(string key, object? value);
    public Error WithMetadata(IReadOnlyDictionary<string, object?> metadata);   // 為 null 時拋出 ArgumentNullException

    public Error CausedBy(Error cause);
    public Error CausedBy(IReadOnlyList<Error> causes);

    public Error GetRootCause();
}
```

- 如果建構子沒有指定 `Type`，預設是 `ErrorType.Validation`。
- `AddFieldError`/`CausedBy` 從不修改呼叫端本身，各自回傳一個新的 `Error`。對共享單例
  （`Error.None`、`Error.NullValue`）呼叫這兩個方法，不會影響到單例本身。
- `AddFieldError(field, messages)` 就算 `messages` 是空的，也會建立這個欄位的項目；而且是
  附加到既有項目後面（而不是取代它）。
- `WithMetadata` 用來附加自由格式的 key/value 資料（例如 retry-after 提示或實體 id），同樣回傳
  一個新的 `Error`。對既有的 key 設值會覆蓋舊值；字典多載則是合併進既有的 metadata。
- `GetRootCause()` 沿著只有單一原因的節點往下走，遇到零個或多個原因（聚合點）的節點時，
  就停下來並回傳該節點。
- 相等性是結構化的：`Code`、`Description`、`Type`、`Fields`（依內容）、`Metadata`（依內容，
  值以 `Equals` 比較）以及 `Causes`（依順序）都必須相符。

```csharp
var error = new Error("User.NotFound", "User was not found", ErrorType.NotFound);

var validation = new Error("Validation", "One or more fields are invalid")
    .AddFieldError("Email", "Email is required");

var wrapped = new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected)
    .CausedBy(new Error("Db.Timeout", "The database connection timed out."));

var limited = new Error("Rate.Limited", "Too many requests.", ErrorType.TooManyRequests)
    .WithMetadata("retryAfterSeconds", 30);
```

## `ErrorType`

一個用名稱分類 `Error`、依值比較的 `readonly record struct`。

```csharp
[JsonConverter(typeof(ErrorTypeJsonConverter))]
public readonly record struct ErrorType
{
    public string Value { get; }

    public ErrorType(string value);   // value 為 null 或空字串時拋出 InvalidErrorTypeException

    public static readonly ErrorType None;
    public static readonly ErrorType NullValue;
    public static readonly ErrorType Validation;
    public static readonly ErrorType NotFound;
    public static readonly ErrorType BadRequest;
    public static readonly ErrorType Unauthorized;
    public static readonly ErrorType Forbidden;
    public static readonly ErrorType Conflict;
    public static readonly ErrorType TooManyRequests;
    public static readonly ErrorType Unexpected;
    public static readonly ErrorType ServiceUnavailable;
}
```

用跟內建分類一樣的方式宣告你自己的分類：

```csharp
public static class OrderErrorType
{
    public static readonly ErrorType OutOfStock = new(nameof(OutOfStock));
}
```

透過 `System.Text.Json` 序列化時，會直接輸出扁平的 `Value` 字串，而不是巢狀物件。

## `ExceptionalError`

一個從捕捉到的 `Exception` 建立出來的 `sealed record ExceptionalError : Error`。

```csharp
public sealed record ExceptionalError : Error
{
    public Exception Exception { get; }

    public static ExceptionalError From(Exception exception);
    public static Error From(Exception exception, Error error);
}
```

- `From(exception)` 把 `Code` 設成例外的型別名稱，`Description` 設成 `exception.Message`
  （`Type` 是 `ErrorType.Unexpected`）。如果這個例外有 `InnerException`，會被遞迴包裝並透過
  `CausedBy` 附加上去。
- `From(exception, error)` 把例外（透過 `From(exception)`）包裝成 `error` 的
  `InnerError`。呈現出來的是 `error` 自己的 `Code`/`Description`，原始例外訊息不會出現
  在主要描述裡。回傳型別是 `Error`，不是 `ExceptionalError`，因為回傳值外層的形狀是
  `error`，不是被包裝的例外。

什麼時候該用哪一個，參考 [包裝捕捉到的例外](../guides/wrapping-exceptions.md)；
`Result.Failure(exception[, error])`（建立在這之上）參考
[Result 與 Result\<TValue\>](result.md)。

## 相關文件

- [建立結構化錯誤](../guides/building-structured-errors.md)
- [Result 與 Error 模型](../concepts/result-and-error-model.md)
- [例外](exceptions.md)：`InvalidErrorTypeException` 等。

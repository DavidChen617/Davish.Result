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
- `AddFieldError`/`WithMetadata`/`CausedBy` 都不修改原本的 `Error`，而是回傳新的 `Error`。對共享單例
  （`Error.None`、`Error.NullValue`）呼叫這些方法，單例本身不受影響。
- `AddFieldError(field, messages)` 即使 `messages` 是空的，也會建立該欄位的項目；欄位已存在時，
  訊息會接在後面，不會取代。
- `WithMetadata` 用來附加任意的 key/value 資料（例如 retry-after 提示或實體 id），同樣回傳
  新的 `Error`。對已有的 key 設值會覆蓋舊值；字典多載會合併進既有的 metadata。
- `GetRootCause()` 沿著單一原因往下走，遇到沒有原因或有多個原因（聚合點）的節點時，
  停下並回傳該節點。
- 依內容比較相等性：`Code`、`Description`、`Type`、`Fields`（依內容，訊息清單比較順序）、
  `Metadata`（依內容，值以 `Equals` 比較）和 `Causes`（比較順序）都相符，才算相等。

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

用名稱替 `Error` 分類的 `readonly record struct`，依值比較。

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

比照內建分類的寫法，宣告你自己的分類：

```csharp
public static class OrderErrorType
{
    public static readonly ErrorType OutOfStock = new(nameof(OutOfStock));
}
```

用 `System.Text.Json` 序列化時，輸出的是 `Value` 字串本身，不是巢狀物件。

## `ExceptionalError`

從捕捉到的 `Exception` 建立的 `sealed record ExceptionalError : Error`。

```csharp
public sealed record ExceptionalError : Error
{
    public Exception Exception { get; }

    public static ExceptionalError From(Exception exception);
    public static Error From(Exception exception, Error error);
}
```

- `From(exception)` 把 `Code` 設成例外的型別名稱，`Description` 設成 `exception.Message`，
  `Type` 設成 `ErrorType.Unexpected`。例外如果有 `InnerException`，會遞迴包裝，再用
  `CausedBy` 掛上去。
- `From(exception, error)` 先用 `From(exception)` 包裝例外，再把它掛在 `error` 底下，成為
  `InnerError`。對外呈現的是 `error` 自己的 `Code`/`Description`，原始例外訊息不會出現在
  描述裡。回傳型別是 `Error` 而不是 `ExceptionalError`，因為最外層是 `error`，不是包裝後的例外。

兩者怎麼選，參考[包裝捕捉到的例外](../guides/wrapping-exceptions.md)。建立在這之上的
`Result.Failure(exception[, error])`，參考[Result 與 Result\<TValue\>](result.md)。

## 相關文件

- [建立結構化錯誤](../guides/building-structured-errors.md)
- [Result 與 Error 模型](../concepts/result-and-error-model.md)
- [例外](exceptions.md)：`InvalidErrorTypeException` 等。

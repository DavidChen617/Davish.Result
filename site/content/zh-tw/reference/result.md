---
title: Result 與 Result<TValue>
weight: 10
---

命名空間：`Davish.Result`（套件 `Davish.Result`）。

## `Result`

代表一個操作的結果，成功或是帶著 `Error` 的失敗。

```csharp
public class Result
{
    public bool IsSuccess { get; }     // Error == Error.None
    public Error Error { get; }

    protected Result(Error error);

    public void Deconstruct(out bool isSuccess, out Error error);

    public static Result Success();
    public static Result Failure(Error error);
    public static Result Failure(Exception exception);
    public static Result Failure(Exception exception, Error error);

    public static Result<TValue> Success<TValue>(TValue value) where TValue : notnull;
    public static Result<TValue> Failure<TValue>(Error error) where TValue : notnull;
    public static Result<TValue> Failure<TValue>(Exception exception) where TValue : notnull;
    public static Result<TValue> Failure<TValue>(Exception exception, Error error) where TValue : notnull;
}
```

- `IsSuccess` 是從 `Error == Error.None` 算出來的，沒有另外儲存一個旗標。
- 建構子是 `protected`，只能透過靜態工廠方法建立實例，或繼承 `Result` 自訂結果型別。
- 如果 `error == Error.None`，`Failure(error)` 會拋出 `InvalidResultStateException`。
- `Failure(exception)` 透過 `ExceptionalError.From(exception)` 建立錯誤。`Error.Description` 會暴露原始例外訊息，相關的安全性提醒請參考
  [Error、ErrorType 與 ExceptionalError](error.md)。
- `Failure(exception, error)` 透過 `ExceptionalError.From(exception, error)` 建立錯誤，
  例外會變成 `error` 的 `InnerError`，而不是取代它的描述。

```csharp
Result ok = Result.Success();
Result bad = Result.Failure(new Error("User.Locked", "The account is locked"));
```

## `Result<TValue>`

代表一個成功時會產生 `TValue` 型別值的操作結果。繼承自 `Result`。

```csharp
public class Result<TValue> : Result where TValue : notnull
{
    public TValue Value { get; }   // 非成功時拋出 ResultValueUnavailableException

    protected internal Result(TValue? value, Error error);

    public void Deconstruct(out bool isSuccess, out TValue? value, out Error error);

    public static implicit operator Result<TValue>(TValue? value);   // null -> 帶 Error.NullValue 的失敗
    public static implicit operator Result<TValue>(Error error);
}
```

- 在非成功的結果上讀取 `Value` 會拋出 `ResultValueUnavailableException`（帶著 `Error`）。
  請先檢查 `IsSuccess`，或改用永遠不會拋例外的解構。
- 如果 `error == Error.None` 且 `value` 為 `null`，建構子會拋出
  `InvalidResultStateException`，因為成功必須帶有非 null 的值。
- 解構是直接讀取底層欄位（不是 `Value`），所以失敗時 `value` 是 `default`，不會拋例外。
- 隱含的 `TValue?` 轉換，讓工廠方法可以直接 `return value;`：非 null 的值會變成
  `Success(value)`，`null` 則變成 `Failure<TValue>(Error.NullValue)`。

```csharp
Result<int> value = Result.Success(42);
Result<int> failed = Result.Failure<int>(new Error("Parse.Failed", "Not a number"));

Result<string> GetName(int id) => id <= 0
    ? new Error("Id.Invalid", "Id must be positive")   // Error -> 失敗結果
    : "David";                                          // value -> 成功結果
```

## 相關文件

- [建立 Result](../guides/creating-results.md)
- [Result 與 Error 模型](../concepts/result-and-error-model.md)

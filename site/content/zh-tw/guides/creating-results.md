---
title: 建立 Result
weight: 10
---

## 非泛型 `Result`（沒有值）

```csharp
Result ok = Result.Success();
Result bad = Result.Failure(new Error("User.Locked", "The account is locked"));
```

## `Result<TValue>`（成功時帶著一個值）

```csharp
Result<int> value = Result.Success(42);
Result<int> failed = Result.Failure<int>(new Error("Parse.Failed", "Not a number"));
```

`Result.Failure(Error.None)` 以及 `Result.Success<TValue>` 傳入 `null` 值都會拋出
`InvalidResultStateException`，因為成功/失敗的狀態必須內部一致。參考
[Result 與 Error 模型](../concepts/result-and-error-model.md)。

## 讀取結果

```csharp
if (result.IsSuccess)
    Use(result.Value);           // Result<T>.Value 在失敗時會拋出 ResultValueUnavailableException
else
    Log(result.Error.Description);
```

`ResultValueUnavailableException` 跟 `InvalidResultStateException` 都衍生自 `ResultException`，
所以 `catch (ResultException)` 兩種都能處理。

`Result` 跟 `Result<TValue>` 都支援解構：

```csharp
var (isSuccess, error) = result;                  // Result
var (isSuccess, value, error) = typedResult;       // Result<TValue> —— 失敗時 value 是 default，不會拋例外
```

## 從值或 Error 隱含轉換

一個值或一個 `Error` 都可以隱含轉換成 result，所以工廠方法可以直接 `return`：

```csharp
Result<string> GetName(int id)
{
    if (id <= 0)
        return new Error("Id.Invalid", "Id must be positive");   // Error  -> 失敗結果

    return "David";                                              // value  -> 成功結果
}
```

`null` 值會轉成帶著 `Error.NullValue` 的失敗結果，而不是帶著 `null` 的成功結果，因為
`Result<TValue>` 要求 `TValue : notnull`。

## 相關文件

- [建立結構化錯誤](building-structured-errors.md)：欄位層級的訊息與因果鏈。
- [包裝捕捉到的例外](wrapping-exceptions.md)：`Result.Failure(exception)` 與
  `Result.Failure(exception, error)`。
- [Result 與 Result\<TValue\>](../reference/result.md)：完整的工廠方法清單。

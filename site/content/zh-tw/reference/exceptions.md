---
title: 例外
weight: 70
---

這個函式庫直接拋出的例外，都衍生自抽象類別
`ResultException : InvalidOperationException`（命名空間 `Davish.Result`），所以
`catch (ResultException)` 都接得住。

## 來自 `Davish.Result`

| 例外 | 什麼時候會拋出 |
|---|---|
| `InvalidResultStateException` | 呼叫 `Result.Failure(error)`/`Result.Failure<TValue>(error)` 時傳入 `Error.None`；或建構 `Result<TValue>` 時 `error == Error.None` 且值為 `null`。例外帶有 `IsSuccess`（原本要建立的狀態）和 `Error`。 |
| `ResultValueUnavailableException` | 在失敗的結果上讀取 `Result<TValue>.Value`。例外帶有 `Error`。 |
| `InvalidErrorTypeException` | 用 `null` 或空字串的 `value` 呼叫 `new ErrorType(value)`。 |

```csharp
public abstract class ResultException : InvalidOperationException
{
    protected ResultException(string message);
}

public sealed class InvalidResultStateException : ResultException
{
    public bool IsSuccess { get; }
    public Error Error { get; }
}

public sealed class ResultValueUnavailableException : ResultException
{
    public Error Error { get; }
}

public sealed class InvalidErrorTypeException : ResultException
{
}
```

## 相關文件

- [Result 與 Error 模型](../concepts/result-and-error-model.md)
- [建立 Result](../guides/creating-results.md)

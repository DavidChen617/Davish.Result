---
title: 包裝捕捉到的例外
weight: 40
---

`ExceptionalError` 把捕捉到的例外包裝成 `Error`，並把例外的 `InnerException` 鏈轉成
`Error.Causes`。

## `Result.Failure(exception)`：快，但可能洩漏細節

```csharp
try
{
    return Result.Success(File.ReadAllText(path));
}
catch (Exception ex)
{
    return Result.Failure<string>(ex);   // Error.Description 是原始的 ex.Message
}
```

`Result.Failure(exception)` 會把 `Error.Description` 設成原始的 `Exception.Message`，裡面可能有
實作細節，例如 SQL 片段、檔案路徑、內部型別名稱。如果這個結果可能出現在 HTTP 回應裡，
而你不希望不受信任的客戶端看到例外訊息，就不要用這個多載。

## `Result.Failure(exception, error)`：對客戶端安全

```csharp
catch (Exception ex)
{
    return Result.Failure<Order>(ex, new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected));
}

// result.Error.Description               -> "Could not load the order."  (可以安全地公開)
// result.Error.InnerError.Description     -> 原始的 ex.Message           (除非你自己讀取，否則不會公開)
```

對外呈現的是你提供的 `Error`，例外只掛在它底下，成為 `InnerError`，所以原始例外訊息
不會出現在 `Error.Description` 裡。

## 把原始例外找回來做記錄

用模式比對（pattern matching）比對 `ExceptionalError`，就能取回原始的 `Exception`。
用 `Result.Failure(exception)` 建立的，比對 `result.Error` 本身；用
`Result.Failure(exception, error)` 建立的，比對 `result.Error.InnerError`：

```csharp
if (result.Error.InnerError is ExceptionalError { Exception: var ex })
    logger.LogError(ex, "Operation failed");   // 完整的 stack trace、例外型別等等
```

## 相關文件

- [Result 與 Error 模型](../concepts/result-and-error-model.md)：`ExceptionalError`
  在因果鏈裡的位置。
- [Error、ErrorType 與 ExceptionalError](../reference/error.md)：`ExceptionalError.From`
  的多載。

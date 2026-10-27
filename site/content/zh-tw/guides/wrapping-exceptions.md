---
title: 包裝捕捉到的例外
weight: 40
---

`ExceptionalError` 把捕捉到的例外包裝成一個 `Error`，並把它的 `InnerException` 鏈對應到
`Error.Causes` 上。

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

`Result.Failure(exception)` 會把 `Error.Description` 設成原始的 `Exception.Message`，這可能
包含實作細節（SQL 片段、檔案路徑、內部型別名稱）。如果這個結果有可能傳到 HTTP 回應，而且你不
希望不受信任的客戶端看到例外訊息，就要避免用這個多載。

## `Result.Failure(exception, error)`：對客戶端安全

```csharp
catch (Exception ex)
{
    return Result.Failure<Order>(ex, new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected));
}

// result.Error.Description               -> "Could not load the order."  (可以安全地公開)
// result.Error.InnerError.Description     -> 原始的 ex.Message           (除非你自己讀取，否則不會公開)
```

結果呈現的是你提供的 `Error`，例外只會附加成它的 `InnerError`，因此原始的例外訊息不會
出現在主要的 `Error.Description` 裡。

## 把原始例外找回來做記錄

用模式比對（pattern match）`ExceptionalError`，就能把原始的 `Exception` 實例找回來。若用的是
`Result.Failure(exception)`，比對 `result.Error` 本身；若用的是
`Result.Failure(exception, error)`，比對 `result.Error.InnerError`：

```csharp
if (result.Error.InnerError is ExceptionalError { Exception: var ex })
    logger.LogError(ex, "Operation failed");   // 完整的 stack trace、例外型別等等
```

## 相關文件

- [Result 與 Error 模型](../concepts/result-and-error-model.md)：`ExceptionalError`
  怎麼融入因果鏈模型。
- [Error、ErrorType 與 ExceptionalError](../reference/error.md)：`ExceptionalError.From`
  的多載。

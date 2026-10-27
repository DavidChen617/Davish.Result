---
title: Davish.Result
---

Davish.Result 是 .NET 的 Result pattern 函式庫。它把操作的成功或失敗表示成一個值
（`Result` / `Result<TValue>`），預期中的錯誤不必靠例外處理。另一個套件可以把 `Result`
直接轉成 ASP.NET Core Minimal API 的回應。

## 安裝

```bash
dotnet add package Davish.Result                    # Result / Result<T> / Error / ErrorType
dotnet add package Davish.Result.AspNetCore.Http     # Result -> Minimal API IResult
```

所有型別都在同一個根命名空間：

```csharp
using Davish.Result;
```

## 從哪裡開始

- **[版本](versions.md)**：目前的套件版本與升級注意事項。
- **[快速上手](getting-started.md)**：安裝核心套件，建立並讀取你的第一個 `Result`。
- **概念**：這套函式庫怎麼組成、為什麼這樣設計：
  - [Result 與 Error 模型](concepts/result-and-error-model.md)：成功與失敗如何保持一致、結構化錯誤、
    因果鏈，以及如何把例外接進來。
  - [ASP.NET Core 整合模型](concepts/aspnetcore-integration-model.md)：`Result` 如何變成 `IResult`，
    以及失敗處理和例外對應各自負責什麼。
  - [錯誤類型對應狀態碼](concepts/status-code-mapping.md)：為什麼對應表是 process-wide 的靜態設定，
    以及重新設定時會發生什麼事。
- **指南**：依任務分類的操作步驟：
  - [建立 Result](guides/creating-results.md)
  - [建立結構化錯誤](guides/building-structured-errors.md)
  - [包裝捕捉到的例外](guides/wrapping-exceptions.md)
  - [設定 ASP.NET Core 整合](guides/setting-up-aspnetcore-integration.md)
  - [把 Result 轉成 HTTP 回應](guides/converting-results-to-http-responses.md)
  - [自訂失敗處理邏輯](guides/customizing-failure-handling.md)
  - [把例外對應到 Result](guides/mapping-exceptions-to-results.md)
  - [設定狀態碼](guides/configuring-status-codes.md)
- **參考文件**：
  - [Result 與 Result\<TValue\>](reference/result.md)
  - [Error、ErrorType 與 ExceptionalError](reference/error.md)
  - [ASP.NET Core 設定](reference/aspnetcore-setup.md)
  - [Minimal API 結果型別](reference/minimal-api-results.md)
  - [狀態碼對應](reference/status-code-mapping.md)
  - [例外](reference/exceptions.md)

## 套件一覽

| 套件 | 用途 | 目標框架 |
|---|---|---|
| `Davish.Result` | 核心的 `Result`/`Result<TValue>`、`Error`/`ErrorType`，以及把例外轉成 `Error` 的機制。 | `netstandard2.0`, `net10.0` |
| `Davish.Result.AspNetCore.Http` | 把 `Result`/`Result<T>` 轉成 Minimal API 的 `IResult`，提供可替換的失敗處理、例外對應到 `Result`，以及錯誤類型對應狀態碼的設定。 | `net10.0` |

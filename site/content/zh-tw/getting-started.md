---
title: 快速上手
weight: 10
---

這一頁帶你走一遍最基本的用法：安裝核心套件，用 `Result<TValue>` 表示預期中的失敗、不再拋出例外，
最後讀出結果。

## 安裝

```bash
dotnet add package Davish.Result
```

`Davish.Result` 的目標框架是 `netstandard2.0` 和 `net10.0`。

## 用 Result 取代拋出例外

```csharp
using Davish.Result;

Result<Order> FindOrder(int orderId) =>
    orderId == 1
        ? Result.Success(new Order(orderId, 100m))
        : Result.Failure<Order>(new Error("Order.NotFound", $"Order {orderId} was not found.", ErrorType.NotFound));

record Order(int Id, decimal Amount);
```

`Result.Success(value)` 建立帶有 `value` 的成功結果，`Result.Failure<TValue>(error)`
建立帶有 `Error` 的失敗結果。像「找不到訂單」這種預期中、可以處理的失敗，就不必拋出例外。

## 讀出結果

```csharp
var result = FindOrder(orderId: 999);

if (result.IsSuccess)
    Console.WriteLine($"Order #{result.Value.Id}: ${result.Value.Amount}");
else
    Console.WriteLine($"Failed: {result.Error.Code} - {result.Error.Description}");
```

讀取 `Value` 前要先檢查 `IsSuccess`：在失敗的結果上讀取 `Value`，會拋出
`ResultValueUnavailableException`。改用解構（deconstruction）就不會拋例外：

```csharp
var (isSuccess, value, error) = result;   // isSuccess 為 false 時，value 是 default
```

## 接下來可以看

- 需要帶欄位訊息的驗證錯誤？或想保留底層原因，同時對外呈現更高層級的錯誤？參考
  [建立結構化錯誤](guides/building-structured-errors.md)。
- 正在寫 ASP.NET Core Minimal API，想把 `Result` 直接轉成 HTTP 回應？參考
  [設定 ASP.NET Core 整合](guides/setting-up-aspnetcore-integration.md)（需要
  `Davish.Result.AspNetCore.Http`）。

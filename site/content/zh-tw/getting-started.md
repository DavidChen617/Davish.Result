---
title: 快速上手
weight: 10
---

這裡示範最小可用的工作流程：安裝核心套件、用 `Result<TValue>` 取代拋出例外來表示預期中的失敗，
然後把它讀回來。

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

`Result.Success(value)` 建立一個帶著 `value` 的成功結果。`Result.Failure<TValue>(error)`
建立一個帶著 `Error` 的失敗結果。這種預期中、可復原的失敗不會拋出例外。

## 把結果讀回來

```csharp
var result = FindOrder(orderId: 999);

if (result.IsSuccess)
    Console.WriteLine($"Order #{result.Value.Id}: ${result.Value.Amount}");
else
    Console.WriteLine($"Failed: {result.Error.Code} - {result.Error.Description}");
```

讀取 `Value` 之前要先檢查 `IsSuccess`，在失敗的結果上讀取 `Value` 會拋出
`ResultValueUnavailableException`。解構（deconstruction）則永遠不會拋例外：

```csharp
var (isSuccess, value, error) = result;   // 當 isSuccess 為 false 時，value 是 default
```

## 接下來可以看

- 需要驗證風格、帶欄位訊息的錯誤，或是想保留底層原因、同時對外呈現更高層級的錯誤？參考
  [建立結構化錯誤](guides/building-structured-errors.md)。
- 正在寫 ASP.NET Core Minimal API，想把 `Result` 直接轉成 HTTP 回應？參考
  [設定 ASP.NET Core 整合](guides/setting-up-aspnetcore-integration.md)（需要
  `Davish.Result.AspNetCore.Http`）。

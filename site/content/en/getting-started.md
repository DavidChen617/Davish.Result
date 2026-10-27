---
title: Getting started
weight: 10
---

This walks through the smallest useful workflow: install the core package, return a
`Result<TValue>` instead of throwing for an expected failure, and read it back.

## Install

```bash
dotnet add package Davish.Result
```

`Davish.Result` targets `netstandard2.0` and `net10.0`.

## Return a Result instead of throwing

```csharp
using Davish.Result;

Result<Order> FindOrder(int orderId) =>
    orderId == 1
        ? Result.Success(new Order(orderId, 100m))
        : Result.Failure<Order>(new Error("Order.NotFound", $"Order {orderId} was not found.", ErrorType.NotFound));

record Order(int Id, decimal Amount);
```

`Result.Success(value)` creates a successful result carrying `value`. `Result.Failure<TValue>(error)`
creates a failed result carrying an `Error` — no exception is thrown for this expected,
recoverable failure.

## Read it back

```csharp
var result = FindOrder(orderId: 999);

if (result.IsSuccess)
    Console.WriteLine($"Order #{result.Value.Id}: ${result.Value.Amount}");
else
    Console.WriteLine($"Failed: {result.Error.Code} - {result.Error.Description}");
```

Check `IsSuccess` before reading `Value` — `Value` throws `ResultValueUnavailableException` on
a failed result. Deconstruction is an alternative that never throws:

```csharp
var (isSuccess, value, error) = result;   // value is default when isSuccess is false
```

## Where to go next

- Need validation-style errors with per-field messages, or to preserve a low-level cause while
  surfacing a higher-level error? See [Building structured errors](guides/building-structured-errors.md).
- Building an ASP.NET Core Minimal API and want to turn a `Result` straight into an HTTP
  response? See [Setting up the ASP.NET Core integration](guides/setting-up-aspnetcore-integration.md)
  (requires `Davish.Result.AspNetCore.Http`).

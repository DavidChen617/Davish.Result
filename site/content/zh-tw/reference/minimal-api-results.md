---
title: Minimal API 結果型別
weight: 50
---

命名空間：`Davish.Result`（套件 `Davish.Result.AspNetCore.Http`）。需要先完成
[ASP.NET Core 設定](aspnetcore-setup.md) 的註冊。

## `ResultToMinimalResultExtension`

核心的成功形狀（完整的參數列表請看 `ResultToMinimalResultExtension` 的 XML 文件）：

```csharp
public static MinimalApiOkResult ToOk(this Result result);
public static MinimalApiNoContentResult ToNoContent(this Result result);
public static MinimalApiCreatedResult ToCreated(this Result result, string routeName, object? routeValues = null);
public static MinimalApiAcceptedResult ToAccepted(this Result result, string? uri = null);

public static MinimalApiOkResult<T> ToOk<T>(this Result<T> result) where T : notnull;
public static MinimalApiNoContentResult ToNoContent<T>(this Result<T> result) where T : notnull;
public static MinimalApiCreatedResult<T> ToCreated<T>(this Result<T> result, string routeName, Func<T, object?>? routeValues = null) where T : notnull;
public static MinimalApiAcceptedResult<T> ToAccepted<T>(this Result<T> result, string? uri = null) where T : notnull;
```

| 方法 | 成功時的狀態碼 | 備註 |
|---|---|---|
| `ToOk()` | `200 OK` | 泛型多載會把值寫成回應 body。 |
| `ToNoContent()` | `204 No Content` | 泛型與非泛型共用同一個型別，`204` 不會有 body。 |
| `ToCreated(routeName, routeValues)` | `201 Created` | 透過 `TypedResults.CreatedAtRoute` 建立 `Location` header。泛型多載的 `routeValues` 是 `Func<T, object?>`，用成功的值建立出來。 |
| `ToAccepted(uri)` | `202 Accepted` | `uri` 是選填的；有給的話會變成 `Location` header。 |

其他形狀，各自對應同名的 `TypedResults` 工廠方法：

| 群組 | 方法 | 結果型別 |
|---|---|---|
| Created / Accepted 變體 | `ToCreated()`、`ToCreatedAtLocation(string \| Uri \| Func<T, string \| Uri>)`、`ToCreated(routeName, RouteValueDictionary)`、`ToAccepted(Uri)`、`ToAcceptedAtRoute(...)` | `MinimalApiCreatedAtLocationResult[<T>]`、`MinimalApiCreatedResult[<T>]`、`MinimalApiAcceptedResult[<T>]`、`MinimalApiAcceptedAtRouteResult[<T>]` |
| 文字 / 內容 | `ToText(...)`、`ToContent(...)` | `MinimalApiContentResult[<T>]` |
| JSON | `ToJson(options \| JsonSerializerContext \| JsonTypeInfo<T>)`（僅泛型） | `MinimalApiJsonResult<T>` |
| Server-sent events | 作用在 `Result<IAsyncEnumerable<string \| T \| SseItem<T>>>` 上的 `ToServerSentEvents(...)` | `MinimalApiServerSentEventsResult<T>`、`MinimalApiSseItemResult<T>` |
| 檔案 / 串流 | `ToBytes`、`ToFile`、`ToPhysicalFile`、`ToVirtualFile`、`ToStream` | `MinimalApiFileResult[<T>]` |
| 重新導向 | `ToRedirect`、`ToLocalRedirect`、`ToRedirectToRoute` | `MinimalApiRedirectResult[<T>]` |
| 驗證 | `ToSignIn`、`ToSignOut`、`ToChallenge`、`ToForbid` | `MinimalApiAuthenticationResult[<T>]` |
| 單純狀態碼 | `ToStatus(int statusCode)` | `MinimalApiStatusCodeResult` |

驗證相關的形狀，以及在要求 range processing 或 `ETag`/`Last-Modified` 時的檔案/串流形狀，成功時
`StatusCode` 是 `null`，因為真正的狀態碼要到執行時才決定。參考
[把 Result 轉成 HTTP 回應](../guides/converting-results-to-http-responses.md)。

上面每一個方法，失敗都一律交給已註冊的 `IMinimalApiFailureHandler` 處理（見下方）。

## `MinimalApiResult` / `MinimalApiResult<TValue>`

公開的抽象基底類別，實作了 `IResult`。繼承它們就能加上自己的成功形狀，同時重用內建的失敗
處理邏輯。

```csharp
public abstract class MinimalApiResult : IResult, IStatusCodeHttpResult
{
    protected MinimalApiResult(Result result);
    protected abstract int SuccessStatusCode { get; }
    protected abstract IResult CreateSuccessResult();
    public int? StatusCode { get; }   // 成功時是 SuccessStatusCode，失敗時是 Error.Type.ToStatusCode()
    public Task ExecuteAsync(HttpContext httpContext);
}

public abstract class MinimalApiResult<TValue> : IResult, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<TValue>
    where TValue : notnull
{
    protected MinimalApiResult(Result<TValue> result);
    protected abstract int SuccessStatusCode { get; }
    protected abstract IResult CreateSuccessResult(TValue value);
    public int? StatusCode { get; }
    public Task ExecuteAsync(HttpContext httpContext);
    // IValueHttpResult<TValue>.Value 在失敗時拋出 ResultValueUnavailableException
    // IValueHttpResult.Value 在失敗時回傳 null，不會拋例外
}
```

上面 `ToXxx()` 方法所回傳的內建形狀，都是這些基底類別的
`public sealed` 實作；不要直接建構它們，請使用 `ToXxx()` 這些擴充方法。

## `IFailureHandler<TResult>` / `IMinimalApiFailureHandler`

```csharp
public interface IFailureHandler<TResult>
{
    ValueTask<TResult> HandleAsync(Result result, CancellationToken cancellationToken);
}

public interface IMinimalApiFailureHandler : IFailureHandler<IResult>;
```

`HandleAsync` 只會在 `Result` 失敗時被呼叫。實作 `IMinimalApiFailureHandler`，再透過
`MinimalApiResultOptions.MinimalApiFailureHandler<THandler>()` 註冊，就能替換掉預設的處理器。
參考 [自訂失敗處理邏輯](../guides/customizing-failure-handling.md)。

預設實作（`DefaultMinimalApiFailureHandler`，internal）用 `error.Type.ToStatusCode()` 產生
`TypedResults.Problem(...)`；如果 `Error.Fields` 不是空的，則改用
`TypedResults.ValidationProblem(...)`（把 `error.Fields` 當成 `errors` 字典）。
`Error.Causes`/`InnerError` 永遠不會出現在回應 body 裡。

## 相關文件

- [把 Result 轉成 HTTP 回應](../guides/converting-results-to-http-responses.md)
- [自訂失敗處理邏輯](../guides/customizing-failure-handling.md)
- [ASP.NET Core 整合模型](../concepts/aspnetcore-integration-model.md)

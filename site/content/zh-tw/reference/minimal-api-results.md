---
title: Minimal API 結果型別
weight: 50
---

命名空間：`Davish.Result`（套件 `Davish.Result.AspNetCore.Http`）。使用前需要先完成
[ASP.NET Core 設定](aspnetcore-setup.md)的註冊。

## `ResultToMinimalResultExtension`

核心的成功形狀如下，完整參數請看 `ResultToMinimalResultExtension` 的 XML 文件註解：

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
| `ToCreated(routeName, routeValues)` | `201 Created` | 透過 `TypedResults.CreatedAtRoute` 產生 `Location` header。泛型多載的 `routeValues` 是 `Func<T, object?>`，由成功的值產生。 |
| `ToAccepted(uri)` | `202 Accepted` | `uri` 為選填；有提供時會成為 `Location` header。 |

其他形狀各自對應同名的 `TypedResults` 工廠方法：

| 群組 | 方法 | 結果型別 |
|---|---|---|
| Created / Accepted 變體 | `ToCreated()`、`ToCreatedAtLocation(string \| Uri \| Func<T, string \| Uri>)`、`ToCreated(routeName, RouteValueDictionary)`、`ToAccepted(Uri)`、`ToAcceptedAtRoute(...)` | `MinimalApiCreatedAtLocationResult[<T>]`、`MinimalApiCreatedResult[<T>]`、`MinimalApiAcceptedResult[<T>]`、`MinimalApiAcceptedAtRouteResult[<T>]` |
| 文字 / 內容 | `ToText(...)`、`ToContent(...)` | `MinimalApiContentResult[<T>]` |
| JSON | `ToJson(options \| JsonSerializerContext \| JsonTypeInfo<T>)`（僅泛型） | `MinimalApiJsonResult<T>` |
| Server-sent events | 作用在 `Result<IAsyncEnumerable<string \| T \| SseItem<T>>>` 上的 `ToServerSentEvents(...)` | `MinimalApiServerSentEventsResult<T>`、`MinimalApiSseItemResult<T>` |
| 檔案 / 串流 | `ToBytes`、`ToFile`、`ToPhysicalFile`、`ToVirtualFile`、`ToStream` | `MinimalApiFileResult[<T>]` |
| 重新導向 | `ToRedirect`、`ToLocalRedirect`、`ToRedirectToRoute` | `MinimalApiRedirectResult[<T>]` |
| 身分驗證 | `ToSignIn`、`ToSignOut`、`ToChallenge`、`ToForbid` | `MinimalApiAuthenticationResult[<T>]` |
| 單純狀態碼 | `ToStatus(int statusCode)` | `MinimalApiStatusCodeResult` |

身分驗證的形狀，以及要求 range processing 或 `ETag`/`Last-Modified` 時的檔案/串流形狀，成功時
`StatusCode` 是 `null`，因為真正的狀態碼要到執行時才知道。參考
[把 Result 轉成 HTTP 回應](../guides/converting-results-to-http-responses.md)。

上面所有方法失敗時，都交給已註冊的 `IMinimalApiFailureHandler` 處理（見下方）。

## `MinimalApiResult` / `MinimalApiResult<TValue>`

實作 `IResult` 的公開抽象基底類別。繼承它們，就能加上自己的成功形狀，同時沿用內建的失敗
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

`ToXxx()` 回傳的內建形狀，都是這些基底類別的 `public sealed` 實作。請透過 `ToXxx()`
擴充方法取得，不要直接建構。

## `IFailureHandler<TResult>` / `IMinimalApiFailureHandler`

```csharp
public interface IFailureHandler<TResult>
{
    ValueTask<TResult> HandleAsync(Result result, CancellationToken cancellationToken);
}

public interface IMinimalApiFailureHandler : IFailureHandler<IResult>;
```

只有 `Result` 失敗時才會呼叫 `HandleAsync`。實作 `IMinimalApiFailureHandler`，再用
`MinimalApiResultOptions.MinimalApiFailureHandler<THandler>()` 註冊，就能替換預設的處理器。
參考[自訂失敗處理邏輯](../guides/customizing-failure-handling.md)。

預設實作是 internal 的 `DefaultMinimalApiFailureHandler`：用 `error.Type.ToStatusCode()` 產生
`TypedResults.Problem(...)`；如果 `Error.Fields` 不是空的，改用
`TypedResults.ValidationProblem(...)`，並把 `error.Fields` 當成 `errors` 字典。
`Error.Causes`/`InnerError` 不會出現在回應 body 裡。

## 相關文件

- [把 Result 轉成 HTTP 回應](../guides/converting-results-to-http-responses.md)
- [自訂失敗處理邏輯](../guides/customizing-failure-handling.md)
- [ASP.NET Core 整合模型](../concepts/aspnetcore-integration-model.md)

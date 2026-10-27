---
title: ASP.NET Core 整合模型
weight: 30
---

`Davish.Result.AspNetCore.Http`（目標框架只有 `net10.0`）負責兩件事：把 `Result`/`Result<T>` 轉成
Minimal API 的 `IResult`；你的程式碼沒有捕捉到的例外，也能進入同一套失敗處理流程。

## 兩個方向的轉換

兩個轉換方向各自獨立，只在失敗處理器（failure handler）會合。套件裡
`IFailureHandler<TResult>` 和 `IExceptionMapper<TSource>` 兩個介面，就是照這個方式切分的。

- **`Result` → `IResult`**（Minimal API 專屬）：`ToOk()`/`ToCreated()`/`ToAccepted()`/`ToNoContent()`，
  以及其他 `ToXxx()` 形狀（文字、JSON、檔案、重新導向、身分驗證等），成功時立刻轉換；
  失敗時交給目前註冊的 `IMinimalApiFailureHandler` 處理。
- **`Exception` → `Result`**（跟 host 無關）：`MapExceptionToResult<TException>(...)` 註冊一條
  轉換規則。`TException`（或其子類別）沒被捕捉、往外拋出時，這條規則會把它轉成 `Result`，
  交給**同一個**失敗處理器，跟直接回傳 `Result.Failure(...)` 走的路一樣。

## 成功立刻寫入，失敗延後處理

`ToOk()` 等方法背後的基底類別是 `MinimalApiResult`/`MinimalApiResult<TValue>`，
它們的 `IResult.ExecuteAsync` 實作如下：

```csharp
public async Task ExecuteAsync(HttpContext httpContext)
{
    var httpResult = _result.IsSuccess
        ? CreateSuccessResult()
        : await ResolveFailureAsync(httpContext);   // 在執行當下才從 DI 解析 IMinimalApiFailureHandler

    await httpResult.ExecuteAsync(httpContext);
}
```

失敗處理器是在執行當下，透過 `httpContext.RequestServices` 解析的，不是在呼叫 `ToOk()` 時。
因此，只要換掉註冊的 `IMinimalApiFailureHandler`，所有端點的失敗回應都會跟著改變，
端點程式碼完全不用動。

## 哪些型別開放擴充，是刻意設計的

`MinimalApiResult`/`MinimalApiResult<TValue>` 刻意設計成 `public abstract`。你可以繼承它們，
加上自己的成功回應形狀（例如 `206 Partial Content`），同時沿用它們的失敗處理邏輯。

內建的形狀則是 `public sealed`。它們是預設實作，不是擴充點：

- `MinimalApiOkResult`、`MinimalApiCreatedResult`、`MinimalApiCreatedAtLocationResult`
- `MinimalApiAcceptedResult`、`MinimalApiAcceptedAtRouteResult`、`MinimalApiNoContentResult`
- `MinimalApiContentResult`、`MinimalApiJsonResult`、`MinimalApiFileResult`
- `MinimalApiRedirectResult`、`MinimalApiAuthenticationResult`、`MinimalApiStatusCodeResult`
- server-sent events 的形狀
- 以及上述型別帶值的泛型 `<T>` 版本

`IFailureHandler<TResult>` 設計成泛型，是為了讓未來其他 host 的失敗處理器（例如 MVC 的
`IActionResult`）也能沿用同一套抽象。目前只有 `IMinimalApiFailureHandler = IFailureHandler<IResult>`
這一種。

## `ToOk()` 等方法回傳具體型別，不是 `IResult`

`ToOk()` 回傳 `MinimalApiOkResult`/`MinimalApiOkResult<T>`，不是 `IResult`，因為 ASP.NET Core
產生 OpenAPI metadata 時需要這樣：`IEndpointMetadataProvider.PopulateMetadata` 是依端點委派*宣告*的
回傳型別呼叫的，回傳型別有實作 `IEndpointMetadataProvider`，成功回應的 metadata 才會登記上去。
如果把回傳型別宣告成 `IResult`，這份 metadata 就會悄悄消失。

每個具體型別本身都實作了 `IResult`，所以只寫 `return result.ToOk();` 的呼叫端，
程式碼不需要修改（不是 source-breaking change）。

## 例外對應依繼承關係比對，最具體的優先

`MapExceptionToResult<TException>(...)` 的比對方式跟 `catch` 子句一樣：從拋出例外的執行期型別開始，
沿著基底型別往上找。如果基底型別和衍生型別都註冊了規則，拋出的是衍生型別時，
會優先套用衍生型別的規則。比對結果會依具體的例外型別快取起來。

## 別忘了 `app.UseExceptionHandler()`

`AddResultAspNetCore` 只註冊 DI 服務，不會把 ASP.NET Core 的例外處理 middleware 接進管線。
要在管線裡加上 `app.UseExceptionHandler()`，`MapExceptionToResult` 的規則才會生效。

`AddResultAspNetCore` 內部也會呼叫 `services.AddProblemDetails()`。不帶參數的
`app.UseExceptionHandler()` 在啟動時需要這個 fallback，用來處理沒有任何規則對應得到的例外。
少了它，應用程式在啟動時就會拋出例外，而不是等到有請求進來才出問題。

實際操作步驟請參考
[設定 ASP.NET Core 整合](../guides/setting-up-aspnetcore-integration.md)、
[把 Result 轉成 HTTP 回應](../guides/converting-results-to-http-responses.md)，以及
[把例外對應到 Result](../guides/mapping-exceptions-to-results.md)。

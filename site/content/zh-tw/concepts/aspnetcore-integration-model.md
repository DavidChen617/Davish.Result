---
title: ASP.NET Core 整合模型
weight: 30
---

`Davish.Result.AspNetCore.Http`（目標框架僅有 `net10.0`）把 `Result`/`Result<T>` 橋接到
Minimal API 的 `IResult`，並且讓你自己的程式碼沒有捕捉到的例外也能流進同一套失敗處理管線。

## 兩個方向的轉換

這套設計（可以參考套件裡 `IFailureHandler<TResult>`/`IExceptionMapper<TSource>` 的切分方式）
讓兩個轉換方向各自獨立，只在失敗處理器（failure handler）這一點會合：

- **`Result` → `IResult`**（Minimal API 專屬）：`ToOk()`/`ToCreated()`/`ToAccepted()`/`ToNoContent()`
  以及其他 `ToXxx()` 形狀（文字、JSON、檔案、重新導向、驗證等等）會立刻轉換成功的情況；失敗則交給目前註冊的 `IMinimalApiFailureHandler` 處理。
- **`Exception` → `Result`**（跟 host 無關）：`MapExceptionToResult<TException>(...)` 註冊一個
  轉換規則，當 `TException`（或其子類別）沒被捕捉而往外拋時就會執行，把它轉成一個 `Result`，
  再流進跟直接回傳 `Result.Failure(...)` **相同**的失敗處理器。

## 成功會立刻寫入，失敗則延後處理

`MinimalApiResult`/`MinimalApiResult<TValue>`（也就是 `ToOk()` 等方法背後的基底類別）把
`IResult.ExecuteAsync` 實作成：

```csharp
public async Task ExecuteAsync(HttpContext httpContext)
{
    var httpResult = _result.IsSuccess
        ? CreateSuccessResult()
        : await ResolveFailureAsync(httpContext);   // 在執行當下才從 DI 解析 IMinimalApiFailureHandler

    await httpResult.ExecuteAsync(httpContext);
}
```

因為失敗的解析是在執行當下透過 `httpContext.RequestServices` 進行（不是在呼叫 `ToOk()`
的當下），所以更換註冊的 `IMinimalApiFailureHandler` 就能改變每個端點的失敗回應，完全不用動到
端點程式碼。

## public/internal 的切分是刻意的

`MinimalApiResult`/`MinimalApiResult<TValue>` 特意設計成 `public abstract`，讓你可以繼承它們，
加上自己的成功回應形狀（例如 `206 Partial Content`），同時重用它們的失敗處理邏輯。內建的形狀（`MinimalApiOkResult`、
`MinimalApiCreatedResult`、`MinimalApiCreatedAtLocationResult`、`MinimalApiAcceptedResult`、
`MinimalApiAcceptedAtRouteResult`、`MinimalApiNoContentResult`、`MinimalApiContentResult`、
`MinimalApiJsonResult`、`MinimalApiFileResult`、`MinimalApiRedirectResult`、
`MinimalApiAuthenticationResult`、`MinimalApiStatusCodeResult`、server-sent events 的形狀，
以及這些帶有值的泛型 `<T>` 版本）則是 `public sealed`，它們是預設實作，不是擴充點。`IFailureHandler<TResult>` 之所以泛型化，是為了讓未來針對特定 host
的失敗處理器（例如 MVC 的 `IActionResult`）可以重用同一套抽象；目前只存在
`IMinimalApiFailureHandler = IFailureHandler<IResult>` 這一種。

## `ToOk()` 等方法回傳的是具體型別，不是 `IResult`

`ToOk()` 回傳的是 `MinimalApiOkResult`/`MinimalApiOkResult<T>`，不是 `IResult`。這是 ASP.NET Core
OpenAPI metadata 的必要條件：`IEndpointMetadataProvider.PopulateMetadata` 是根據端點委派*宣告*
的回傳型別來呼叫的，只有實作 `IEndpointMetadataProvider` 的型別，成功回應的 metadata 才會被登記。
如果把回傳型別宣告成 `IResult`，這份 metadata 就會悄悄消失。因為每個具體型別本身都實作了
`IResult`，對只是寫 `return result.ToOk();` 的呼叫端來說，這不算是 source-breaking 的改動。

## 例外對應是依繼承關係比對，最具體的優先

`MapExceptionToResult<TException>(...)` 的註冊會沿著被拋出例外的執行期型別的基底型別鏈往上比對
（跟 `catch` 子句比對的方式一樣），從最衍生的型別開始找。如果同時為一個基底型別跟一個衍生型別
都註冊了對應規則，衍生型別的規則會在該衍生型別的實例上優先套用。解析結果會依每個具體例外型別
快取。

## `app.UseExceptionHandler()` 是必要的，而且很容易被忘記

`AddResultAspNetCore` 只註冊 DI 服務，不會自動把 ASP.NET Core 的例外處理 middleware
接進管線。`MapExceptionToResult` 的註冊只有在 pipeline 裡加了 `app.UseExceptionHandler()`
之後才會真正生效。`AddResultAspNetCore` 內部也會呼叫 `services.AddProblemDetails()`，這是
ASP.NET Core 沒有帶參數的 `app.UseExceptionHandler()` 在啟動時需要的 fallback，用來處理沒有
任何已註冊例外對應規則能處理的例外。沒有這個 fallback，應用程式會在啟動時就拋出例外，
而不是等到請求發生時才出問題。

以任務為導向的操作說明參考
[設定 ASP.NET Core 整合](../guides/setting-up-aspnetcore-integration.md)、
[把 Result 轉成 HTTP 回應](../guides/converting-results-to-http-responses.md)，以及
[把例外對應到 Result](../guides/mapping-exceptions-to-results.md)。

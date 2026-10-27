---
title: 把 Result 轉成 HTTP 回應
weight: 60
---

這裡假設 ASP.NET Core 整合已經設定完成，參考
[設定 ASP.NET Core 整合](setting-up-aspnetcore-integration.md)。

## 基本轉換

```csharp
app.MapGet("/bookings/{id}", (int id, BookingService service) =>
    service.Find(id).ToOk());                              // 200 OK，或失敗處理器的回應

app.MapPost("/bookings", (CreateBooking request, BookingService service) =>
    service.Create(request).ToCreated("GetBooking", b => new { id = b.Id }));  // 201 Created

app.MapDelete("/bookings/{id}", (int id, BookingService service) =>
    service.Delete(id).ToNoContent());                     // 204 No Content，或失敗處理器的回應
```

| 方法 | 成功時 |
|---|---|
| `ToOk()` | `200 OK` |
| `ToNoContent()` | `204 No Content` |
| `ToCreated(routeName, routeValues)` | 在具名 route 上回 `201 Created` |
| `ToCreated()` | `201 Created`，沒有 `Location`（僅非泛型） |
| `ToCreatedAtLocation(string \| Uri)` | `201 Created`，帶明確的 `Location` |
| `ToCreatedAtLocation(value => string \| Uri)` | 同上，`Location` 由值建立（僅泛型） |
| `ToAccepted(string \| Uri)` | `202 Accepted`，`Location` 為選填 |
| `ToAcceptedAtRoute(routeName, routeValues)` | 在具名 route 上回 `202 Accepted` |
| `ToText(...)` / `ToContent(...)` | `200 OK`（或指定的 `statusCode`），文字 body |
| `ToJson(options \| context \| typeInfo)` | `200 OK`（或指定的 `statusCode`），用明確指定的序列化器序列化值（僅泛型） |
| `ToServerSentEvents(...)` | `200 OK` `text/event-stream`，作用在 `Result<IAsyncEnumerable<string \| T \| SseItem<T>>>` 上 |
| `ToBytes(...)` / `ToFile(...)` / `ToPhysicalFile(...)` / `ToVirtualFile(...)` / `ToStream(...)` | `200 OK` 的檔案或串流回應；泛型多載接受 `Func<T, ...>` 來提供內容或路徑 |
| `ToRedirect(...)` / `ToLocalRedirect(...)` / `ToRedirectToRoute(...)` | 依 `permanent` 與 `preserveMethod` 回 `301`/`302`/`307`/`308` |
| `ToSignIn(...)` / `ToSignOut(...)` | 透過驗證 handler 登入 / 登出 |
| `ToChallenge(...)` / `ToForbid(...)` | 透過驗證 handler 進行 challenge / forbid |
| `ToStatus(statusCode)` | 只帶指定狀態碼的空回應 |

多數都有非泛型（`Result`）與泛型（`Result<T>`，帶著值）兩種多載；上面標明的那幾個則只在其中一邊
有意義。`ToText`/`ToContent` 的泛型多載接受 `Func<T, string>`，從值建立 body。
`ToCreatedAtLocation` 的名稱跟 `ToCreated(routeName, ...)` 不同，是因為後者若加一個 `string`
多載，會跟它無法區分。

每一種形狀都對應到同名的 `TypedResults` 工廠方法，所以回應本身跟內建的完全一致。當真正的狀態碼
要到執行時才決定，該形狀的 `StatusCode` 屬性在成功時會是 `null`（未知），而不是亂猜：
`ToSignIn`/`ToSignOut`/`ToChallenge`/`ToForbid`（由驗證 handler 決定，例如 cookie handler 會是
`302`），以及要求 range processing 或 `ETag`/`Last-Modified` 時的檔案/串流形狀
（可能是 `206`/`304`/`412`）。其他情況下都是確切的值。

`ToChallenge`/`ToForbid` 會把回應交給驗證 handler（導向登入頁、`WWW-Authenticate` 等），
這跟由 `ErrorType.Unauthorized`/`Forbidden` 對應出來的失敗結果不同，後者一律產生
`ProblemDetails`。失敗一律由已註冊的 `IMinimalApiFailureHandler` 用同一套方式處理。

## 成功會立刻寫入，失敗則交給已註冊的處理器

`ToOk()` 等方法回傳的是具體的公開型別（`MinimalApiOkResult<T>` 之類），不是 `IResult`，
這樣 ASP.NET Core 的 OpenAPI 產生器才能正確描述成功回應。基底類型
（`MinimalApiResult`/`MinimalApiResult<TValue>`）也是公開的，所以你可以繼承它們、加上自己的
成功形狀（例如 `206 Partial Content`），同時重用它們的失敗處理邏輯。

## 從泛型轉換結果讀取值

`MinimalApiResult<TValue>` 實作了 `IValueHttpResult<TValue>`（失敗時拋出
`ResultValueUnavailableException`，跟 `Result<TValue>.Value` 本身一樣）以及非泛型的
`IValueHttpResult`（失敗時回傳 `null`，而不是拋例外）。如果需要在 `ExecuteAsync` 之外讀取這個
值，記得先檢查底層結果的 `StatusCode`/`IsSuccess`。

## 相關文件

- [ASP.NET Core 整合模型](../concepts/aspnetcore-integration-model.md)：為什麼成功會立刻
  寫入而失敗會延後，以及為什麼回傳型別是具體型別。
- [Minimal API 結果型別](../reference/minimal-api-results.md)：完整簽章。

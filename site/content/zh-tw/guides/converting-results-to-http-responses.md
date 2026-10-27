---
title: 把 Result 轉成 HTTP 回應
weight: 60
---

本頁假設你已經完成 ASP.NET Core 整合的設定，還沒設定的話請先看
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
| `ToSignIn(...)` / `ToSignOut(...)` | 透過身分驗證 handler 登入 / 登出 |
| `ToChallenge(...)` / `ToForbid(...)` | 透過身分驗證 handler 進行 challenge / forbid |
| `ToStatus(statusCode)` | 只帶指定狀態碼的空回應 |

多數方法都有非泛型（`Result`）和泛型（`Result<T>`，帶有值）兩種多載；表中標註「僅泛型」或
「僅非泛型」的，只在其中一邊有意義。`ToText`/`ToContent` 的泛型多載接受 `Func<T, string>`，
從值產生 body。

`ToCreatedAtLocation` 沒有沿用 `ToCreated` 這個名字，是因為 `ToCreated(routeName, ...)`
如果再加一個 `string` 多載，兩者會無法區分。

每種形狀都對應到同名的 `TypedResults` 工廠方法，所以實際寫出的回應跟內建的完全一致。

有些情況要到執行時才知道真正的狀態碼，這時成功結果的 `StatusCode` 屬性是 `null`（未知），
不會隨便猜一個值：

- `ToSignIn`/`ToSignOut`/`ToChallenge`/`ToForbid`：由身分驗證 handler 決定，例如 cookie handler
  會回 `302`。
- 檔案和串流形狀，在要求 range processing 或 `ETag`/`Last-Modified` 時：可能是 `206`/`304`/`412`。

其他情況下，`StatusCode` 都是確切的值。

`ToChallenge`/`ToForbid` 會把回應交給身分驗證 handler，由它導向登入頁、加上 `WWW-Authenticate`
標頭等。這跟 `ErrorType.Unauthorized`/`Forbidden` 的失敗結果不同：失敗一律交給已註冊的
`IMinimalApiFailureHandler` 處理，預設處理器產生的是 `ProblemDetails`。

## 成功立刻寫入，失敗交給已註冊的處理器

`ToOk()` 等方法回傳具體的公開型別（例如 `MinimalApiOkResult<T>`），不是 `IResult`，
這樣 ASP.NET Core 的 OpenAPI 產生器才能正確描述成功回應。基底類別
`MinimalApiResult`/`MinimalApiResult<TValue>` 也是公開的，你可以繼承它們，加上自己的
成功形狀（例如 `206 Partial Content`），同時沿用它們的失敗處理邏輯。

## 從泛型轉換結果讀取值

`MinimalApiResult<TValue>` 實作了兩個介面，失敗時的行為不同：

- `IValueHttpResult<TValue>`：失敗時拋出 `ResultValueUnavailableException`，跟
  `Result<TValue>.Value` 一樣。
- 非泛型的 `IValueHttpResult`：失敗時回傳 `null`，不拋例外。

如果要在 `ExecuteAsync` 之外讀取這個值，記得先檢查底層結果的 `StatusCode`/`IsSuccess`。

## 相關文件

- [ASP.NET Core 整合模型](../concepts/aspnetcore-integration-model.md)：為什麼成功立刻寫入、
  失敗延後處理，以及為什麼回傳具體型別。
- [Minimal API 結果型別](../reference/minimal-api-results.md)：完整簽章。

# Result.AspNetCore.Http — Exception Mapping / Failure Handling 設計

來源：使用者貼的 `Result.AspNetCore` spec 草稿，跟現有 `Result.AspNetCore.Http` 對照、討論、微調後的定案。

## 目標

1. `ToOk()`/`ToCreated()`/... 這些 Minimal API 回傳型別可以被 client 擴充（實作我們沒提供的 success shape）。
2. Exception → Result 的翻譯機制不綁定 Minimal API（host-agnostic，以後可以平行支援 MVC）。
3. Failure 分支（Result → HTTP 回應）可以被 client 整個換掉。
4. Exception 可以被轉換成 Result，加入既有的 Failure 處理管線。

## 邊界定義

- **Result（core）**：純域模型，不知道 HTTP/Exception，不動。
- **Result.AspNetCore.Http（現有套件，不拆新套件）**：涵蓋兩個方向的翻譯：
  - `Exception → Result`：`MapExceptionToResult` / `IExceptionMapper<TSource>` / `IExceptionHandler` —— **host-agnostic**，MVC/Minimal API 都適用。
  - `Result → IResult`：既有的 `ToOk`/`ToCreated`/`ToAccepted`/`ToNoContent`（Success）+ `IFailureHandler`（Failure）—— **Minimal-API-specific**（`IResult` 是 Minimal API 專屬型別，MVC 用 `IActionResult`）。
  - 兩條路徑最終都匯流到同一個 `IFailureHandler`。
- **Endpoint**：只選 Success semantic，不手動處理 Failure。

這個邊界直接反映在 DI 進入點的巢狀結構上：外層 `AddResultAspNetCore` 是通用入口，Minimal-API-specific 的東西收在巢狀的 `AddMinimalApiResult` 裡，以後要支援 MVC 就平行加一個 `ConfigureMvcResult`，不用動外層。

## 完整組裝

依賴順序由下往上排列，是目前唯一一份最新程式碼，其餘章節只講「為什麼」，程式碼一律看這裡。

### Exception → Result

```csharp
public interface IExceptionMapper<in TSource> where TSource : Exception
{
    ValueTask<Result> MapAsync(HttpContext context, TSource source, CancellationToken cancellationToken);
}

internal interface IExceptionMapperDispatcher
{
    ValueTask<Result?> MapAsync(HttpContext context, object source, CancellationToken cancellationToken);
}

internal sealed class ExceptionMapperDispatcher(
    IReadOnlyDictionary<Type, Func<HttpContext, Exception, CancellationToken, ValueTask<Result>>> mappers)
    : IExceptionMapperDispatcher
{
    private readonly ConcurrentDictionary<Type, Func<HttpContext, Exception, CancellationToken, ValueTask<Result>>?> _cache = new();

    public async ValueTask<Result?> MapAsync(HttpContext context, object source, CancellationToken cancellationToken)
    {
        var exception = (Exception)source;
        var mapper = _cache.GetOrAdd(exception.GetType(), Resolve);

        return mapper is null ? null : await mapper(context, exception, cancellationToken);
    }

    private Func<HttpContext, Exception, CancellationToken, ValueTask<Result>>? Resolve(Type type)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            if (mappers.TryGetValue(t, out var mapper))
                return mapper;
        }
        return null;
    }
}
```

### Failure Handling

```csharp
public interface IFailureHandler<TResult>
{
    ValueTask<TResult> HandleAsync(Result result, CancellationToken cancellationToken);
}

public interface IMinimalApiFailureHandler : IFailureHandler<IResult>;

internal sealed class DefaultMinimalApiFailureHandler : IMinimalApiFailureHandler
{
    public ValueTask<IResult> HandleAsync(Result result, CancellationToken cancellationToken)
    {
        var error = result.Error;

        IResult httpResult = error.Fields.Count > 0
            ? Results.ValidationProblem(
                title: error.Code,
                detail: error.Description,
                errors: error.Fields.Select(x => new KeyValuePair<string, string[]>(x.Key, x.Value.ToArray())))
            : Results.Problem(
                title: error.Code,
                detail: error.Description,
                statusCode: error.Type.ToStatusCode());

        return ValueTask.FromResult(httpResult);
    }
}
```

### Exception pipeline：MinimalApiExceptionHandler

```csharp
internal sealed class MinimalApiExceptionHandler(
    IExceptionMapperDispatcher dispatcher,
    IMinimalApiFailureHandler failureHandler) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var result = await dispatcher.MapAsync(context, exception, cancellationToken);
        if (result is null)
            return false;

        var httpResult = await failureHandler.HandleAsync(result, cancellationToken);
        await httpResult.ExecuteAsync(context);
        return true;
    }
}
```

完全不需要 `IEndpointMetadataProvider`/`IStatusCodeHttpResult`——沒有單一 endpoint 的宣告回傳型別可以掛 metadata，這是跨 endpoint 的全域 middleware。

### Minimal API 直接回傳：MinimalApiResult<TValue>

```csharp
public abstract class MinimalApiResult<TValue> : IResult, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<TValue>
{
    private readonly Result<TValue> result;

    protected MinimalApiResult(Result<TValue> result) => this.result = result;

    protected abstract int SuccessStatusCode { get; }
    protected abstract IResult CreateSuccessResult(TValue value);

    public int? StatusCode => result.IsSuccess ? SuccessStatusCode : result.Error.Type.ToStatusCode();

    TValue IValueHttpResult<TValue>.Value => result.Value; // Failure 時跟 Result<T>.Value 一樣丟 ResultValueUnavailableException（沿用既有契約，不是新契約）
    object? IValueHttpResult.Value => result.IsSuccess ? result.Value : null; // 非泛型版本用 null 比丟例外合理

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var httpResult = result.IsSuccess
            ? CreateSuccessResult(result.Value)
            : await ResolveFailureAsync(httpContext);

        await httpResult.ExecuteAsync(httpContext);
    }

    private async Task<IResult> ResolveFailureAsync(HttpContext httpContext)
    {
        var handler = httpContext.RequestServices.GetRequiredService<IMinimalApiFailureHandler>();
        return await handler.HandleAsync(result, httpContext.RequestAborted);
    }
}

internal sealed class MinimalApiOkResult<TValue>(Result<TValue> result)
    : MinimalApiResult<TValue>(result), IEndpointMetadataProvider
{
    protected override int SuccessStatusCode => StatusCodes.Status200OK;
    protected override IResult CreateSuccessResult(TValue value) => TypedResults.Ok(value);

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        Ok<TValue>.PopulateMetadata(method, builder); // 借用內建型別自己的 success metadata，不重複刻
}
```

**現有套件其實有 8 個方法，不是 4 個**：`ToOk`/`ToCreated`/`ToAccepted`/`ToNoContent` 各自有掛在**非泛型** `Result` 上（沒有 value）跟掛在**泛型** `Result<T>` 上（帶 value）兩份（`ResultToMinimalResultExtension.cs` 現況如此）。上面的 `MinimalApiResult<TValue>` 只涵蓋泛型那一半，非泛型那一半需要一個平行、獨立的抽象基底——**不要**硬把兩者接成繼承關係（`CreateSuccessResult()` 沒有參數 vs. 帶 `TValue value`，簽章對不上，硬接只會製造更複雜的型別體操），照「三行重複比早熟的抽象化好」的原則，直接讓兩個 base class 各自獨立，重複一點點小邏輯：

```csharp
public abstract class MinimalApiResult : IResult, IStatusCodeHttpResult
{
    private readonly Result result;

    protected MinimalApiResult(Result result) => this.result = result;

    protected abstract int SuccessStatusCode { get; }
    protected abstract IResult CreateSuccessResult();

    public int? StatusCode => result.IsSuccess ? SuccessStatusCode : result.Error.Type.ToStatusCode();

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var httpResult = result.IsSuccess
            ? CreateSuccessResult()
            : await ResolveFailureAsync(httpContext);

        await httpResult.ExecuteAsync(httpContext);
    }

    private async Task<IResult> ResolveFailureAsync(HttpContext httpContext)
    {
        var handler = httpContext.RequestServices.GetRequiredService<IMinimalApiFailureHandler>();
        return await handler.HandleAsync(result, httpContext.RequestAborted);
    }

}

internal sealed class MinimalApiOkResult(Result result)
    : MinimalApiResult(result), IEndpointMetadataProvider
{
    protected override int SuccessStatusCode => StatusCodes.Status200OK;
    protected override IResult CreateSuccessResult() => TypedResults.Ok();

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        Ok.PopulateMetadata(method, builder); // 內建非泛型 Ok（無 value）自己的 metadata
}
```

`MinimalApiNoContentResult` **不需要**分泛型/非泛型兩份——204 本來就永遠沒有 body，不管呼叫端是 `Result.ToNoContent()` 還是 `Result<T>.ToNoContent()`，都可以共用同一個非泛型型別（建構子收 `Result`，`Result<T> : Result` 向上轉型即可，`.Value` 從頭到尾用不到）：

```csharp
internal sealed class MinimalApiNoContentResult(Result result)
    : MinimalApiResult(result), IEndpointMetadataProvider
{
    protected override int SuccessStatusCode => StatusCodes.Status204NoContent;
    protected override IResult CreateSuccessResult() => TypedResults.NoContent();

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        NoContent.PopulateMetadata(method, builder);
}

internal sealed class MinimalApiCreatedResult(Result result, string routeName, object? routeValues)
    : MinimalApiResult(result), IEndpointMetadataProvider
{
    protected override int SuccessStatusCode => StatusCodes.Status201Created;
    protected override IResult CreateSuccessResult() => TypedResults.CreatedAtRoute(routeName, routeValues);

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        CreatedAtRoute.PopulateMetadata(method, builder);
}

internal sealed class MinimalApiAcceptedResult(Result result, string? uri)
    : MinimalApiResult(result), IEndpointMetadataProvider
{
    protected override int SuccessStatusCode => StatusCodes.Status202Accepted;
    protected override IResult CreateSuccessResult() => TypedResults.Accepted(uri);

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        Accepted.PopulateMetadata(method, builder);
}

internal sealed class MinimalApiCreatedResult<TValue>(Result<TValue> result, string routeName, Func<TValue, object?>? routeValues)
    : MinimalApiResult<TValue>(result), IEndpointMetadataProvider
{
    protected override int SuccessStatusCode => StatusCodes.Status201Created;
    protected override IResult CreateSuccessResult(TValue value) =>
        TypedResults.CreatedAtRoute(value, routeName, routeValues?.Invoke(value));

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        CreatedAtRoute<TValue>.PopulateMetadata(method, builder);
}

internal sealed class MinimalApiAcceptedResult<TValue>(Result<TValue> result, string? uri)
    : MinimalApiResult<TValue>(result), IEndpointMetadataProvider
{
    protected override int SuccessStatusCode => StatusCodes.Status202Accepted;
    protected override IResult CreateSuccessResult(TValue value) => TypedResults.Accepted(uri, value);

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        Accepted<TValue>.PopulateMetadata(method, builder);
}

public static class ResultToMinimalResultExtension
{
    // 泛型（Result<T>，帶 value）
    public static MinimalApiOkResult<T> ToOk<T>(this Result<T> result) where T : notnull => new(result);
    public static MinimalApiNoContentResult ToNoContent<T>(this Result<T> result) where T : notnull => new(result);
    public static MinimalApiCreatedResult<T> ToCreated<T>(this Result<T> result, string routeName, Func<T, object?>? routeValues = null) where T : notnull =>
        new(result, routeName, routeValues);
    public static MinimalApiAcceptedResult<T> ToAccepted<T>(this Result<T> result, string? uri = null) where T : notnull =>
        new(result, uri);

    // 非泛型（Result，無 value）
    public static MinimalApiOkResult ToOk(this Result result) => new(result);
    public static MinimalApiNoContentResult ToNoContent(this Result result) => new(result);
    public static MinimalApiCreatedResult ToCreated(this Result result, string routeName, object? routeValues = null) =>
        new(result, routeName, routeValues);
    public static MinimalApiAcceptedResult ToAccepted(this Result result, string? uri = null) =>
        new(result, uri);
}
```

型別命名（`Ok`/`Ok<TValue>`/`CreatedAtRoute`/`CreatedAtRoute<TValue>`/`Accepted`/`Accepted<TValue>`/`NoContent`）已用官方文件驗證（[Microsoft.AspNetCore.Http.HttpResults Namespace](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpresults)）——`NoContent` 只有非泛型一個，沒有泛型版本，印證了下面「`MinimalApiNoContentResult` 不分泛型/非泛型」這個決定跟內建設計一致。

型別家族最終長這樣（兩個獨立的 base，`MinimalApiNoContentResult` 共用）：

```text
MinimalApiResult<TValue>          （public abstract，包 Result<TValue>，帶 value）
├── MinimalApiOkResult<TValue>
├── MinimalApiCreatedResult<TValue>
└── MinimalApiAcceptedResult<TValue>

MinimalApiResult                  （public abstract，包 Result，無 value）
├── MinimalApiOkResult
├── MinimalApiCreatedResult
└── MinimalApiAcceptedResult

MinimalApiNoContentResult          （internal sealed，繼承非泛型 MinimalApiResult，泛型/非泛型呼叫共用同一個型別）
```

### Client 擴充範例：自訂 success shape

呼應目標 1（`ToXxx` 可以被擴充）。Client 想要一個我們沒提供的 shape（例如 `206 Partial Content`），只要繼承公開的 `MinimalApiResult<TValue>`，自己補 Success 那半，Failure 分支完全繼承、不用重寫：

```csharp
public sealed class PartialContentMinimalResult<TValue>(Result<TValue> result)
    : MinimalApiResult<TValue>(result), IEndpointMetadataProvider
{
    protected override int SuccessStatusCode => StatusCodes.Status206PartialContent;

    protected override IResult CreateSuccessResult(TValue value) =>
        Results.Content(JsonSerializer.Serialize(value), "application/json", statusCode: 206);

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        builder.Metadata.Add(new ProducesResponseTypeMetadata(206, typeof(TValue)));
}

public static class MyResultExtensions
{
    public static PartialContentMinimalResult<T> ToPartialContent<T>(this Result<T> result) where T : notnull =>
        new(result);
}
```

用法跟內建的 `ToOk()` 完全一致：

```csharp
app.MapGet("/reports/{id}", async (Guid id, IReportService service, CancellationToken ct) =>
{
    var result = await service.GetPartialAsync(id, ct);
    return result.ToPartialContent(); // 成功 → 206；失敗 → 一樣走 IMinimalApiFailureHandler
});
```

Client 只需要寫跟「成功時長什麼樣」有關的三個成員（`SuccessStatusCode`/`CreateSuccessResult`/`PopulateMetadata`），Failure 分支（`ExecuteAsync` 的判斷、`ResolveFailureAsync`、呼叫 `IMinimalApiFailureHandler`）一行都不用寫，也不會跟內建的 `ToOk()`/`ToCreated()` 行為不一致。

### DI 進入點

```csharp
public static class ResultAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddResultAspNetCore(
        this IServiceCollection services,
        Action<ResultAspNetCoreOptions> configureOptions)
    {
        var options = new ResultAspNetCoreOptions(services);
        configureOptions(options);

        services.TryAddSingleton<IExceptionMapperDispatcher>(_ => options.BuildDispatcher());
        services.AddProblemDetails(); // app.UseExceptionHandler() 的必要 fallback，host-agnostic，見「實作紀錄」第 4、10 點

        return services;
    }
}

public sealed class ResultAspNetCoreOptions(IServiceCollection services)
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, CancellationToken, ValueTask<Result>>> _mappers = [];

    public void MapExceptionToResult<TException>(
        Func<HttpContext, TException, Result> mapper)
        where TException : Exception
    {
        _mappers[typeof(TException)] = (context, exception, ct) =>
            ValueTask.FromResult(mapper(context, (TException)exception));
    }

    public void MapExceptionToResult<TException>(
        Func<HttpContext, TException, CancellationToken, ValueTask<Result>> mapper)
        where TException : Exception
    {
        _mappers[typeof(TException)] = (context, exception, ct) =>
            mapper(context, (TException)exception, ct);
    }

    public void MapExceptionToResult<TException, TMapper>()
        where TException : Exception
        where TMapper : class, IExceptionMapper<TException>
    {
        services.TryAddTransient<TMapper>();
        _mappers[typeof(TException)] = (context, exception, ct) =>
        {
            var mapper = context.RequestServices.GetRequiredService<TMapper>();
            return mapper.MapAsync(context, (TException)exception, ct);
        };
    }

    public void AddMinimalApiResult(Action<MinimalApiResultOptions>? configureOptions = null)
    {
        services.TryAddSingleton<IMinimalApiFailureHandler, DefaultMinimalApiFailureHandler>();
        services.AddExceptionHandler<MinimalApiExceptionHandler>(); // 依賴 IMinimalApiFailureHandler，跟它綁在一起註冊（見「實作紀錄」第 8 點）
        configureOptions?.Invoke(new MinimalApiResultOptions(services));
    }

    // 純轉呼叫，不改變 ResultHttpOptions 原本 process-wide 靜態、lock-once 的模型——只是讓消費端能從同一個
    // AddResultAspNetCore(o => ...) 呼叫設定它，不用另外再寫一行 ResultHttpOptions.Configure(...)。
    public void ConfigureStatusCodes(Action<ResultHttpOptionsBuilder> configure) => ResultHttpOptions.Configure(configure);

    // ProblemDetails 是 host-agnostic 的（MVC 的 [ApiController]/ControllerBase.Problem() 用同一份
    // ProblemDetailsOptions），不屬於 Minimal API，所以放在這一層，不是 MinimalApiResultOptions（見「實作紀錄」第 10 點）。
    // 用 services.Configure<T>(...) 疊加設定，不是再呼叫一次 AddProblemDetails。
    public void ConfigureProblemDetails(Action<ProblemDetailsOptions> configure) => services.Configure(configure);

    internal IExceptionMapperDispatcher BuildDispatcher() => new ExceptionMapperDispatcher(_mappers);
}

public sealed class MinimalApiResultOptions(IServiceCollection services)
{
    public void MinimalApiFailureHandler<THandler>() where THandler : class, IMinimalApiFailureHandler
    {
        services.Replace(ServiceDescriptor.Singleton<IMinimalApiFailureHandler, THandler>());
    }
}
```

### 用法

```csharp
builder.Services.AddResultAspNetCore(o =>
{
    o.MapExceptionToResult<NotFoundException>((context, exception) =>
        Result.Failure(new Error("resource.not_found", exception.Message, ErrorType.NotFound)));

    o.ConfigureStatusCodes(c => c.CustomMap = new Dictionary<ErrorType, int> { [ErrorType.NotFound] = 404 });
    o.ConfigureProblemDetails(p => p.CustomizeProblemDetails = ctx => ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);

    o.AddMinimalApiResult(m =>
    {
        m.MinimalApiFailureHandler<CustomerFailureHandler>();
    });
});

// Program.cs 一定要加這行，AddResultAspNetCore 只註冊 DI 服務，不會自動接進 middleware pipeline：
app.UseExceptionHandler();
```

## 設計理由（決策紀錄，非流水帳）

- **ProblemDetails 耦合**：現況 `ToOk()`/`ToCreated()` 直接寫死呼叫 `ToProblemDetail()`/`ToValidationProblemDetail()`。改成 `IFailureHandler`，預設實作仍輸出 ProblemDetails 但可插拔、可被消費端覆寫。這是 breaking change，需要同步改測試。
- **`ToOk()` 等公開方法的回傳型別**：從 `IResult` 改成具體型別（`MinimalApiOkResult<T>`），這是必要的、不是誤動——理由見下方「OpenAPI metadata 限制」。對呼叫端（`return result.ToOk();`）不算 source breaking change，因為具體型別本來就實作 `IResult`，向上轉型永遠合法。
- **`ResultHttpOptions`（ErrorType → status code 的靜態表）不跟這次的 DI 合併**，維持獨立設定（`ResultHttpOptions.Configure(...)`），因為兩者生命週期模型不同（process-wide 靜態 lock-once vs. 標準 DI scope）。`DefaultMinimalApiFailureHandler` 沿用這個既有機制決定 status code，不重新刻一份。
  - **前提限制**：`ResultHttpOptions.Configure(...)` 必須在 `app.MapGet(...)` 之前呼叫，否則 OpenAPI metadata 會缺漏（實際 runtime 回應的 status code 不受影響，因為 `IFailureHandler` 是即時查表，只有 Swagger 文件這個靜態產物會不完整）。
- **`IFailureHandler<TResult>` 泛型化 + 拿掉 `HttpContext` 參數**：
  - 泛型化讓介面可以跨 host 重用：`IMinimalApiFailureHandler = IFailureHandler<IResult>`，以後 MVC 平行加 `IMvcFailureHandler = IFailureHandler<IActionResult>`，不用改基礎介面。兩邊實作內容不能共用，這個泛型化純粹是 API 表面整潔度的好處，成本很低。
  - 拿掉 `HttpContext`：`DefaultMinimalApiFailureHandler` 不受影響（它回傳的 `IResult` 自己的 `ExecuteAsync(HttpContext)` 才是真正寫 response 的地方，`traceId` 這類欄位不受影響）。真正會失去的：自訂 handler 沒辦法在**決定要回傳什麼**的當下讀取 request 資訊（`Accept` header、`HttpContext.User`、臨時 DI 解析）。目前沒有具體場景需要，接受這個取捨；之後如果需要，屬於 breaking change。
- **`IExceptionMapper<TSource>` 約束 `where TSource : Exception`**：原本刻意不限制（保留給以後可能出現的非 Exception 來源），後來確認目前、以及可預見的未來，轉換來源就是 Exception，沒有其他來源，所以把約束加回介面本身，型別系統、名字、實際用途三者一致。
- **`ValueTask<Result>`（`IExceptionMapper`）/`ValueTask<TResult>`（`IFailureHandler`）**，不是 repo 其他地方（`Result.Extension` 的 `Then`/`ThenAsync`）慣用的 `Task<Result>`——因為要掛進 ASP.NET Core 自己的 `IExceptionHandler.TryHandleAsync`（回傳 `ValueTask<bool>`），是每個 request 的熱路徑，多數實作只是同步組一個 `Result`，用 `ValueTask` 省配置成本。
- **DI-based `MapExceptionToResult<TException, TMapper>()` 用 `TryAddTransient`**：不能在註冊當下（`ConfigureServices` 階段）就把 `TMapper` 實例解出來存進 closure，會造成 captive dependency（例如 `TMapper` 依賴 scoped 的 `DbContext` 卻被長壽命 closure 抓住）。正確做法是只登記型別，dispatch 當下才用 `context.RequestServices.GetRequiredService<TMapper>()` 解析。
- **Dispatch 比對策略：沿繼承鏈往上找**（像 `catch (BaseException ex)` 語意），從 `exception.GetType()` 開始往上找 `.BaseType`，找不到回傳 `null`。Exception 是單一繼承（class），往上走永遠是一條線，不會有「多個祖先都有註冊、不知道選哪個」的歧義，天生最具體優先。用 `ConcurrentDictionary<Type, ...>` 做 cache，呼應現有 `ResultHttpOptions.ResolveStatusCode` 的做法。
- **兩條路徑（`MinimalApiResult<TValue>` / `MinimalApiExceptionHandler`）各自獨立的 class，不合併**：`IResult` 是 Minimal API 專屬型別，`IExceptionHandler.TryHandleAsync` 是跨 endpoint 的全域 middleware，不受任何單一 endpoint 宣告回傳型別限制。兩者只在最後都呼叫同一個 `IFailureHandler` 這一點匯流。
- **OpenAPI metadata 限制（已用官方文件驗證，不是猜的）**：`IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)` 是 `static abstract`，只在 `app.MapGet(...)` 註冊 endpoint 的當下呼叫一次，只看得到 `MethodInfo`/`EndpointBuilder`，沒有任何 instance、沒有任何一次 request 發生過。官方文件原文："This is called for each parameter and return type of the route handler or action **with a declared type implementing this interface**."（[IEndpointMetadataProvider.PopulateMetadata](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.metadata.iendpointmetadataprovider.populatemetadata)）
  - 推論 1：宣告回傳型別是關鍵，不是實際型別。`ToOk()` 若宣告回傳 `IResult`，`PopulateMetadata` 永遠不會被呼叫。
  - 推論 2：`PopulateMetadata` 不能讀任何 instance 資料，但可以讀「app 啟動時就已經固定」的全域靜態設定（例如 `ResultHttpOptions` 的表，前提是先於 `app.MapGet(...)` 設定完成）。
  - 因此 Success 的 status code（200/201/202/204）是 per-shape 固定常數，只能靠每個 `ToXxx()` 有自己的具體型別來正確宣告；Failure 的 status code 靠遍歷全域靜態表列舉「所有可能」。沒有「一個共用型別、兩邊都對」的選項。
- **`MinimalApiResult<TValue>` 是 `public abstract`，不是 `internal`**：刻意開放給 client 當擴充基底，讓他們能實作我們沒提供的 success shape（例如 206 Partial Content），同時重用已經寫好的 Failure 解析邏輯，不用整個從零重寫。代價：`protected abstract` 的 `SuccessStatusCode`/`CreateSuccessResult` 從此是正式的公開擴充契約，之後改簽章對外部 consumer 是 breaking change。內建子類維持 `internal sealed`，不開放。
- **不自動宣告 Failure 的 OpenAPI metadata**：一開始考慮過在 `PopulateMetadata` 裡自動遍歷 `ResultHttpOptions` 全域表、把「所有登記過的 status code」都列成這個 endpoint 可能的回應——後來否決了，因為這樣做**不夠精確**：一個 `GET` 端點實際上可能只會失敗成 404/500，自動列舉全域表卻會連 409 Conflict 這種通常只屬於別的 endpoint 的 status code 都一併宣告進去，等於對每個 endpoint 都塞一份「可能誇大」的 metadata。改成不自動宣告，Failure 的 OpenAPI metadata 交給消費端自己視需求在 `app.MapGet(...)` 後面手動加 `.Produces(...)`——這是 ASP.NET Core 本來就提供的標準作法，各 endpoint 能精確描述自己實際會回什麼，不用我們的套件幫忙猜。
- **`DefaultMinimalApiFailureHandler` body 格式完全照舊**（沿用現有 `ToProblemDetail()`/`ToValidationProblemDetail()` 的邏輯，只是搬進 handler 內部，輸出 byte-for-byte 不變）：

  | `Error` 欄位 | 沒有 `Fields` → `Problem(...)` | 有 `Fields` → `ValidationProblem(...)` |
  |---|---|---|
  | `Code` | `title` | `title` |
  | `Description` | `detail` | `detail` |
  | `Type.ToStatusCode()` | `statusCode` | （沒傳，固定回 400） |
  | `Fields` | 不會走到這條路徑 | `errors`（`Dictionary<string,string[]>`） |

  `Error.Causes`/`InnerError` 不會出現在 body 裡（逐欄位手動組，不是整包序列化 `Error`，不會意外洩漏因果鏈細節）。

Source（引用過的官方文件）：
- [IEndpointMetadataProvider.PopulateMetadata Method](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.metadata.iendpointmetadataprovider.populatemetadata)
- [IValueHttpResult\<TValue\> Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.ivaluehttpresult-1)
- [HttpJsonServiceExtensions.ConfigureHttpJsonOptions Method](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.httpjsonserviceextensions.configurehttpjsonoptions)（`ConfigureXxxOptions` 命名慣例的參考依據）

## 測試計畫

比照現有 `tests/Result.AspNetCore.Http.Tests/` 的分法（Unit / Integration / 專門處理靜態全域狀態的 Collection），`Given...When...Then...` 命名。

### `ExceptionMapperDispatcherTests`（unit）

- `GivenExactTypeMapperRegistered_WhenMatchingExceptionThrown_ThenReturnsMappedResult`
- `GivenMapperRegisteredForBaseType_WhenSubclassExceptionThrown_ThenBaseTypeMapperStillMatches`（繼承鏈往上找）
- `GivenMappersRegisteredForBothParentAndGrandparent_WhenSubclassExceptionThrown_ThenMostSpecificMapperWins`
- `GivenNoMatchingMapperAnywhereInChain_WhenExceptionThrown_ThenReturnsNull`
- `GivenSameExceptionTypeResolvedTwice_WhenMapAsyncCalledAgain_ThenCachedResultUsedAndStillCorrect`
- `GivenSyncDelegateMapper_WhenMapAsyncCalled_ThenReturnsExpectedResult`
- `GivenAsyncDelegateMapper_WhenMapAsyncCalled_ThenReturnsExpectedResult`
- `GivenDiBasedMapper_WhenMapAsyncCalled_ThenTMapperResolvedFromRequestServicesAndInvoked`
- `GivenDiBasedMapperWithScopedDependency_WhenResolvedPerRequest_ThenNoCaptiveDependencyException`（驗證 `TryAddTransient` 這個決定沒有製造 captive dependency）

### `MinimalApiExceptionHandlerTests`（unit，mock `IExceptionMapperDispatcher`/`IMinimalApiFailureHandler`）

- `GivenDispatcherReturnsResult_WhenTryHandleAsyncCalled_ThenReturnsTrueAndFailureHandlerInvoked`
- `GivenDispatcherReturnsNull_WhenTryHandleAsyncCalled_ThenReturnsFalseAndFailureHandlerNotInvoked`

### `DefaultMinimalApiFailureHandlerTests`（unit）

- `GivenErrorWithoutFields_WhenHandleAsyncCalled_ThenReturnsProblemWithCodeAsTitleAndStatusCodeFromErrorType`
- `GivenErrorWithFields_WhenHandleAsyncCalled_ThenReturnsValidationProblemWithErrorsDictionary`
- `GivenErrorWithCauses_WhenHandleAsyncCalled_ThenCausesDoNotAppearInBody`（鎖住「不會意外洩漏因果鏈」這個決定）

### `MinimalApiResultTests`（unit，涵蓋 8 個子類，可用 `[Theory]` 參數化跑過 Ok/Created/Accepted/NoContent × 泛型/非泛型）

- `GivenSuccessResult_WhenExecuteAsyncCalled_ThenWritesExpectedStatusCodeAndBody`
- `GivenFailureResult_WhenExecuteAsyncCalled_ThenDelegatesToRegisteredIMinimalApiFailureHandler`
- `GivenSuccessResult_WhenStatusCodePropertyRead_ThenReturnsShapeConstantWithoutExecutingRequest`（`IStatusCodeHttpResult`，純屬性讀取，不用真的執行）
- `GivenFailureResult_WhenStatusCodePropertyRead_ThenReturnsErrorTypeToStatusCode`
- `GivenSuccessResult_WhenGenericValuePropertyRead_ThenReturnsValue`（`IValueHttpResult<TValue>.Value`）
- `GivenFailureResult_WhenGenericValuePropertyRead_ThenThrowsResultValueUnavailableException`（沿用 `Result<T>.Value` 既有契約）
- `GivenFailureResult_WhenNonGenericValuePropertyRead_ThenReturnsNullInsteadOfThrowing`（鎖住這個刻意的不對稱設計）
- `GivenEndpointMappedWithToOk_WhenAppBuilt_ThenEndpointMetadataMatchesBuiltInOkMetadata`（整合測試，驗證 `PopulateMetadata` 委派機制真的有生效，這是設計上花最多力氣驗證的部分，值得專門測）

### `ResultAspNetCoreOptionsTests` / DI 組裝（unit，`ServiceCollection` + `BuildServiceProvider()`）

- `GivenMapExceptionToResultRegistered_WhenServiceProviderBuilt_ThenDispatcherResolvesAndMapsCorrectly`
- `GivenAddMinimalApiResultNotCalled_WhenResolvingIMinimalApiFailureHandler_ThenThrowsInvalidOperationException`（沒呼叫 `AddMinimalApiResult`，預設 handler 沒被註冊，`GetRequiredService` 應該可預期地失敗，不是靜默通過）
- `GivenCustomFailureHandlerRegisteredViaMinimalApiFailureHandler_WhenResolvingIMinimalApiFailureHandler_ThenReturnsCustomNotDefault`（驗證 `services.Replace(...)` 真的換掉，不是疊加兩份註冊）
- `GivenDiBasedExceptionMapperRegistered_WhenResolvedTwice_ThenNewInstanceEachTime`（驗證 `TryAddTransient` 生命週期）

### `ResultAspNetCoreHttpIntegrationTests`（整合測試，比照現有 `ResultAspNetCoreHttpIntegrationTests.cs`，真的架一個 minimal host）

- `GivenEndpointReturnsSuccessResult_WhenRequested_ThenReturns200WithBody`
- `GivenEndpointReturnsFailureResult_WhenRequested_ThenReturnsDefaultProblemDetailsWithCorrectStatusCode`
- `GivenEndpointThrowsMappedException_WhenRequested_ThenProducesSameShapeAsDirectFailureResult`（驗證「兩條路徑最終匯流到同一個 IFailureHandler」這個核心設計目標）
- `GivenEndpointThrowsUnmappedException_WhenRequested_ThenFallsThroughToDefaultAspNetCoreExceptionBehavior`
- `GivenUseExceptionHandlerNotCalled_WhenMappedExceptionThrown_ThenExceptionMappingNeverFires`（鎖住那個「容易被漏掉的前提」，避免以後有人不小心把這個 middleware 拿掉都沒發現）

## 實作紀錄（設計時沒抓到、寫 code 才發現的修正）

已經實作完成（production code + 測試，136 個測試全過），過程中發現以下幾點是 plan.md 原本的設計沒考慮到的，都已修正：

1. **8 個內建子類必須是 `public`，不能是 `internal`**——`ToOk()` 等擴充方法要宣告回傳具體型別（不是 `IResult`）才能讓 `PopulateMetadata` 生效，而 `public` 方法不能回傳 `internal` 型別（CS0050）。原本「四個/八個內建子類維持 internal，不開放」的決定站不住腳，只有兩個抽象基底 (`MinimalApiResult`/`MinimalApiResult<TValue>`) 跟八個具體子類全部都是 `public`；真正不開放的只有 `MetadataHelper`（呼叫 `static abstract` 介面成員的小工具）、`ExceptionMapperDispatcher`、`DefaultMinimalApiFailureHandler`、`MinimalApiExceptionHandler` 這些真正的內部管線。
2. **`static abstract` interface member 不能用 `ConcreteType.Method(...)` 直接呼叫**——`Ok.PopulateMetadata(...)`這種寫法編譯不過（CS0117），因為內建型別是用 explicit interface implementation 實作 `IEndpointMetadataProvider.PopulateMetadata` 的，只能透過泛型約束呼叫。解法是加一個小的 `MetadataHelper.PopulateFrom<T>(method, builder) where T : IEndpointMetadataProvider => T.PopulateMetadata(method, builder);`，各子類呼叫 `MetadataHelper.PopulateFrom<Ok<TValue>>(method, builder)` 而不是 `Ok<TValue>.PopulateMetadata(...)`。
3. **`DefaultMinimalApiFailureHandler` 要用 `TypedResults.Problem`/`TypedResults.ValidationProblem`，不是 `Results.Problem`/`Results.ValidationProblem`**——一開始寫成後者，導致測試斷言 `Assert.IsType<ValidationProblem>(...)` 失敗（實際型別對不上，`Results` 是舊的、回傳型別較鬆散的 API）。
4. **`app.UseExceptionHandler()` 沒有參數時，就算已經註冊了 `IExceptionHandler`，還是需要一個 fallback**（`ExceptionHandlingPath`、`ExceptionHandler` delegate，或 `services.AddProblemDetails()` 三選一），不然 app 啟動時直接丟 `InvalidOperationException`。已在 `AddResultAspNetCore` 內部順手呼叫 `services.AddProblemDetails()`，讓沒被任何 `MapExceptionToResult` 攔到的例外，也能得到一致的 ProblemDetails 回應，而不是讓消費端自己踩到這個啟動期例外（這個呼叫點後來又搬過一次，見第 10 點）。
5. `IExceptionMapperDispatcher.MapAsync` 的參數從 `object source` 簡化成直接 `Exception exception`——反正目前（也是唯一）的呼叫端就是 `Exception`，不需要那層 `object` 間接轉型。
6. `AddResultAspNetCore` 內部改用 ASP.NET Core 自己的 `services.AddExceptionHandler<MinimalApiExceptionHandler>()`，取代手動的 `services.TryAddSingleton<IExceptionHandler, MinimalApiExceptionHandler>()`，是框架自己提供、更符合慣例的註冊方式。
7. **`ResultExceptionHandler` 改名成 `MinimalApiExceptionHandler`，且不加 `Default` 前綴**——它內部依賴的是 `IMinimalApiFailureHandler`，本質上就是 Minimal-API-specific，改名對齊 `MinimalApiResult`/`MinimalApiFailureHandler` 這個命名家族。不加 `Default` 是因為它目前沒有對應的覆寫方法（不像 `DefaultMinimalApiFailureHandler` 有 `m.MinimalApiFailureHandler<THandler>()` 可以換掉），叫 Default 會暗示有替換機制但其實沒有。
8. **`services.AddExceptionHandler<MinimalApiExceptionHandler>()` 從 `AddResultAspNetCore` 移進 `AddMinimalApiResult`**——因為 `MinimalApiExceptionHandler` 依賴 `IMinimalApiFailureHandler`，這個依賴只有呼叫 `AddMinimalApiResult` 才會註冊。原本放在外層的話，如果消費端只呼叫 `AddResultAspNetCore(o => o.MapExceptionToResult<X>(...))` 卻沒呼叫 `AddMinimalApiResult`，`MinimalApiExceptionHandler` 還是會被註冊，但它需要的依賴沒人提供，會在真的發生例外、DI 嘗試建構它的那一刻才爆炸，而不是在啟動時就發現「註冊了一半」的狀態。移進去之後，這兩個東西的註冊綁在一起，不會再有這種半吊子狀態。
9. **`ConfigureProblemDetails(Action<ProblemDetailsOptions>)`**：讓 `services.AddProblemDetails()`（見第 4 點）也能被消費端客製化（例如 `CustomizeProblemDetails` 塞 `traceId`）。用 `services.Configure<ProblemDetailsOptions>(configure)` 疊加設定，不是再呼叫一次 `AddProblemDetails(configure)`——`IOptions` 的 `Configure` 呼叫本來就是疊加式的，不用擔心衝突。
10. **`services.AddProblemDetails()`/`ConfigureProblemDetails` 不該放在 `AddMinimalApiResult`/`MinimalApiResultOptions` 裡，要放回外層的 `AddResultAspNetCore`/`ResultAspNetCoreOptions`**——一開始因為「`AddProblemDetails()` 目前恰好是在 `AddMinimalApiResult` 裡呼叫的」就把它們一起放進巢狀層，但這個理由是錯的：`ProblemDetails`（`ProblemDetailsOptions`）本身是 ASP.NET Core **host-agnostic** 的機制，MVC 的 `[ApiController]`/`ControllerBase.Problem()` 用的是同一份設定，跟 Minimal API 無關。跟最早定下的邊界原則（host-agnostic 放外層、host-specific 放巢狀層）對齊，兩者都移回外層；`services.AddExceptionHandler<MinimalApiExceptionHandler>()`（第 8 點）繼續留在巢狀層，因為那個是真的依賴 `IMinimalApiFailureHandler`、貨真價實 host-specific 的東西。
11. **`AddMinimalApiResult` 的 `configureOptions` 改成可選（`Action<MinimalApiResultOptions>? configureOptions = null`）**——不想覆寫預設 `IMinimalApiFailureHandler`、只是想單純啟用 Minimal API 整合的消費端，原本要寫 `o.AddMinimalApiResult(_ => { })` 這種沒意義的空 lambda，現在可以直接 `o.AddMinimalApiResult()`。

## 待決事項（尚未拍板）

目前沒有未拍板的項目。

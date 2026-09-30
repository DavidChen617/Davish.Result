using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Davish.Result.AspNetCore.Http.Tests;

internal sealed record Item(int Id);

[JsonSerializable(typeof(Item))]
internal sealed partial class ItemJsonContext : JsonSerializerContext;

/// <summary>
/// Tests the success shapes that mirror the remaining <c>TypedResults</c> overloads: <c>Created(location)</c>,
/// <c>AcceptedAtRoute</c>, <c>Text</c>/<c>Content</c>, <c>Json</c> and <c>ServerSentEvents</c>.
/// </summary>
public class MinimalApiSuccessShapesTests
{
    private static readonly Error NotFoundError = new("Item.NotFound", "Item was not found", ErrorType.NotFound);

    private static async Task<DefaultHttpContext> ExecuteAsync(IResult result)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddResultAspNetCore(o => o.AddMinimalApiResult());

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };

        await result.ExecuteAsync(httpContext);
        httpContext.Response.Body.Position = 0;

        return httpContext;
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext httpContext)
    {
        using var reader = new StreamReader(httpContext.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private static async IAsyncEnumerable<T> StreamOf<T>(params T[] items)
    {
        foreach (var item in items)
            yield return item;

        await Task.CompletedTask;
    }

    // ---- 201 Created (Location) ----

    [Fact]
    public async Task GivenSuccessResult_WhenToCreatedWithoutArguments_ThenWrites201WithoutLocation()
    {
        var context = await ExecuteAsync(Result.Success().ToCreated());

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey(HeaderNames.Location));
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToCreatedAtLocationString_ThenWrites201WithLocation()
    {
        var context = await ExecuteAsync(Result.Success().ToCreatedAtLocation("/items/1"));

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        Assert.Equal("/items/1", context.Response.Headers.Location);
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToCreatedAtLocationUri_ThenWrites201WithLocation()
    {
        var context = await ExecuteAsync(Result.Success().ToCreatedAtLocation(new Uri("/items/1", UriKind.Relative)));

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        Assert.Equal("/items/1", context.Response.Headers.Location);
    }

    [Fact]
    public void GivenSuccessResult_WhenToCreatedWithRouteValueDictionary_ThenStatusCodeIs201()
    {
        var httpResult = Result.Success().ToCreated("GetItem", new RouteValueDictionary { ["id"] = 1 });

        Assert.Equal(StatusCodes.Status201Created, httpResult.StatusCode);
    }

    [Fact]
    public async Task GivenFailedResult_WhenToCreatedAtLocation_ThenDelegatesToFailureHandler()
    {
        var context = await ExecuteAsync(Result.Failure(NotFoundError).ToCreatedAtLocation("/items/1"));

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey(HeaderNames.Location));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToCreatedAtLocationString_ThenWritesValueAndLocation()
    {
        var context = await ExecuteAsync(Result.Success(new Item(1)).ToCreatedAtLocation("/items/1"));

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        Assert.Equal("/items/1", context.Response.Headers.Location);
        Assert.Contains("\"id\":1", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToCreatedAtLocationUri_ThenWritesValueAndLocation()
    {
        var context = await ExecuteAsync(
            Result.Success(new Item(1)).ToCreatedAtLocation(new Uri("/items/1", UriKind.Relative)));

        Assert.Equal("/items/1", context.Response.Headers.Location);
        Assert.Contains("\"id\":1", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToCreatedAtLocationStringSelector_ThenLocationIsBuiltFromValue()
    {
        var context = await ExecuteAsync(Result.Success(new Item(7)).ToCreatedAtLocation(i => $"/items/{i.Id}"));

        Assert.Equal("/items/7", context.Response.Headers.Location);
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToCreatedAtLocationUriSelector_ThenLocationIsBuiltFromValue()
    {
        var context = await ExecuteAsync(
            Result.Success(new Item(7)).ToCreatedAtLocation(i => new Uri($"/items/{i.Id}", UriKind.Relative)));

        Assert.Equal("/items/7", context.Response.Headers.Location);
    }

    [Fact]
    public async Task GivenFailedValue_WhenToCreatedAtLocationSelector_ThenSelectorIsNotInvoked()
    {
        var invoked = false;

        var context = await ExecuteAsync(Result.Failure<Item>(NotFoundError).ToCreatedAtLocation(i =>
        {
            invoked = true;
            return $"/items/{i.Id}";
        }));

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    // ---- 202 Accepted ----

    [Fact]
    public async Task GivenSuccessResult_WhenToAcceptedUri_ThenWrites202WithLocation()
    {
        var context = await ExecuteAsync(Result.Success().ToAccepted(new Uri("/items/1/status", UriKind.Relative)));

        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
        Assert.Equal("/items/1/status", context.Response.Headers.Location);
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToAcceptedUri_ThenWritesValueAndLocation()
    {
        var context = await ExecuteAsync(
            Result.Success(new Item(1)).ToAccepted(new Uri("/items/1/status", UriKind.Relative)));

        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
        Assert.Equal("/items/1/status", context.Response.Headers.Location);
        Assert.Contains("\"id\":1", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToAcceptedWithoutArguments_ThenStillBindsToStringOverload()
    {
        var context = await ExecuteAsync(Result.Success().ToAccepted());

        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
    }

    [Fact]
    public void GivenSuccessResult_WhenToAcceptedAtRoute_ThenStatusCodeIs202()
    {
        Assert.Equal(StatusCodes.Status202Accepted, Result.Success().ToAcceptedAtRoute().StatusCode);
        Assert.Equal(StatusCodes.Status202Accepted, Result.Success().ToAcceptedAtRoute("GetItem", new { id = 1 }).StatusCode);
        Assert.Equal(
            StatusCodes.Status202Accepted,
            Result.Success().ToAcceptedAtRoute("GetItem", new RouteValueDictionary { ["id"] = 1 }).StatusCode);
    }

    [Fact]
    public void GivenSuccessValue_WhenToAcceptedAtRoute_ThenStatusCodeIs202()
    {
        var httpResult = Result.Success(new Item(1)).ToAcceptedAtRoute("GetItem", i => new { id = i.Id });

        Assert.Equal(StatusCodes.Status202Accepted, httpResult.StatusCode);
    }

    [Fact]
    public void GivenSuccessValue_WhenToAcceptedAtRouteWithRouteValueDictionary_ThenStatusCodeIs202()
    {
        var httpResult = Result.Success(new Item(1))
            .ToAcceptedAtRoute("GetItem", new RouteValueDictionary { ["id"] = 1 });

        Assert.Equal(StatusCodes.Status202Accepted, httpResult.StatusCode);
    }

    [Fact]
    public async Task GivenFailedValue_WhenToAcceptedAtRoute_ThenRouteValuesFactoryIsNotInvoked()
    {
        var invoked = false;

        var context = await ExecuteAsync(Result.Failure<Item>(NotFoundError).ToAcceptedAtRoute("GetItem", i =>
        {
            invoked = true;
            return new { id = i.Id };
        }));

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    // ---- Text / Content ----

    [Fact]
    public async Task GivenSuccessResult_WhenToText_ThenWritesContent()
    {
        var context = await ExecuteAsync(Result.Success().ToText("pong"));

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.StartsWith("text/plain", context.Response.ContentType);
        Assert.Equal("pong", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToTextWithStatusCode_ThenStatusCodeAndResponseAgree()
    {
        var httpResult = Result.Success().ToText("done", statusCode: StatusCodes.Status202Accepted);
        var context = await ExecuteAsync(httpResult);

        Assert.Equal(StatusCodes.Status202Accepted, httpResult.StatusCode);
        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToTextUtf8_ThenWritesBytes()
    {
        var context = await ExecuteAsync(Result.Success().ToText(Encoding.UTF8.GetBytes("哈囉")));

        Assert.Equal("哈囉", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToTextSelector_ThenContentIsBuiltFromValue()
    {
        var context = await ExecuteAsync(Result.Success(new Item(3)).ToText(i => $"item-{i.Id}"));

        Assert.Equal("item-3", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToTextUtf8Selector_ThenContentIsBuiltFromValue()
    {
        var context = await ExecuteAsync(
            Result.Success(new Item(3)).ToText(i => (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes($"item-{i.Id}")));

        Assert.Equal("item-3", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToContentWithContentType_ThenWritesContentType()
    {
        var context = await ExecuteAsync(Result.Success().ToContent("<p>hi</p>", "text/html", Encoding.UTF8));

        Assert.StartsWith("text/html", context.Response.ContentType);
        Assert.Equal("<p>hi</p>", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessResult_WhenToContentWithMediaTypeHeaderValue_ThenWritesContentType()
    {
        var context = await ExecuteAsync(
            Result.Success().ToContent("<p>hi</p>", new MediaTypeHeaderValue("text/html")));

        Assert.StartsWith("text/html", context.Response.ContentType);
        Assert.Equal("<p>hi</p>", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToContentSelector_ThenContentIsBuiltFromValue()
    {
        var context = await ExecuteAsync(Result.Success(new Item(5)).ToContent(i => $"<b>{i.Id}</b>", "text/html"));

        Assert.StartsWith("text/html", context.Response.ContentType);
        Assert.Equal("<b>5</b>", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToContentSelectorWithMediaTypeHeaderValue_ThenContentIsBuiltFromValue()
    {
        var context = await ExecuteAsync(
            Result.Success(new Item(5)).ToContent(i => $"<b>{i.Id}</b>", new MediaTypeHeaderValue("text/html")));

        Assert.Equal("<b>5</b>", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenFailedValue_WhenToTextSelector_ThenSelectorIsNotInvokedAndFailureHandlerRuns()
    {
        var invoked = false;

        var context = await ExecuteAsync(Result.Failure<Item>(NotFoundError).ToText(i =>
        {
            invoked = true;
            return "x";
        }));

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public void GivenFailedResult_WhenToTextWithStatusCode_ThenStatusCodeReflectsErrorType()
    {
        var httpResult = Result.Failure(NotFoundError).ToText("x", statusCode: StatusCodes.Status202Accepted);

        Assert.Equal(StatusCodes.Status404NotFound, httpResult.StatusCode);
    }

    // ---- Json ----

    [Fact]
    public async Task GivenSuccessValue_WhenToJsonWithOptions_ThenSerializesWithThoseOptions()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper };
        var context = await ExecuteAsync(Result.Success(new Item(1)).ToJson(options));

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("\"ID\":1", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToJsonWithoutOptions_ThenUsesDefaults()
    {
        var context = await ExecuteAsync(Result.Success(new Item(1)).ToJson());

        Assert.Contains("\"id\":1", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToJsonWithContext_ThenSerializes()
    {
        var context = await ExecuteAsync(Result.Success(new Item(1)).ToJson(ItemJsonContext.Default));

        Assert.Contains("\"Id\":1", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenSuccessValue_WhenToJsonWithTypeInfoAndContentTypeAndStatus_ThenAllAreApplied()
    {
        var httpResult = Result.Success(new Item(1)).ToJson(
            ItemJsonContext.Default.Item, "application/vnd.test+json", StatusCodes.Status201Created);
        var context = await ExecuteAsync(httpResult);

        Assert.Equal(StatusCodes.Status201Created, httpResult.StatusCode);
        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        Assert.Equal("application/vnd.test+json", context.Response.ContentType);
        Assert.Contains("\"Id\":1", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task GivenFailedValue_WhenToJson_ThenDelegatesToFailureHandler()
    {
        var context = await ExecuteAsync(Result.Failure<Item>(NotFoundError).ToJson());

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Contains(NotFoundError.Code, await ReadBodyAsync(context));
    }

    // ---- Server-Sent Events ----

    [Fact]
    public async Task GivenSuccessStrings_WhenToServerSentEvents_ThenWritesEventStream()
    {
        var context = await ExecuteAsync(Result.Success(StreamOf("a", "b")).ToServerSentEvents("tick"));

        Assert.Equal("text/event-stream", context.Response.ContentType);
        var body = await ReadBodyAsync(context);
        Assert.Contains("event: tick", body);
        Assert.Contains("data: a", body);
        Assert.Contains("data: b", body);
    }

    [Fact]
    public async Task GivenSuccessItems_WhenToServerSentEvents_ThenWritesEventStream()
    {
        var context = await ExecuteAsync(Result.Success(StreamOf(new Item(1), new Item(2))).ToServerSentEvents());

        Assert.Equal("text/event-stream", context.Response.ContentType);
        var body = await ReadBodyAsync(context);
        Assert.Contains("\"id\":1", body);
        Assert.Contains("\"id\":2", body);
    }

    [Fact]
    public async Task GivenSuccessSseItems_WhenToServerSentEvents_ThenWritesEventStreamWithItemEventType()
    {
        var items = StreamOf(new SseItem<int>(1, "first"), new SseItem<int>(2, "second"));
        var context = await ExecuteAsync(Result.Success(items).ToServerSentEvents());

        var body = await ReadBodyAsync(context);
        Assert.Contains("event: first", body);
        Assert.Contains("event: second", body);
    }

    [Fact]
    public async Task GivenFailedStream_WhenToServerSentEvents_ThenDelegatesToFailureHandler()
    {
        var context = await ExecuteAsync(Result.Failure<IAsyncEnumerable<string>>(NotFoundError).ToServerSentEvents());

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }
}

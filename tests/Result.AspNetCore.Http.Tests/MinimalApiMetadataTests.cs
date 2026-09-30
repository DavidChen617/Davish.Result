using System.Net.ServerSentEvents;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace Davish.Result.AspNetCore.Http.Tests;

/// <summary>
/// Verifies the OpenAPI-facing endpoint metadata (<see cref="IProducesResponseTypeMetadata"/>) that each success
/// shape contributes via <c>IEndpointMetadataProvider</c>, by asserting it is identical to what the equivalent
/// built-in <c>TypedResults</c> return type contributes. Nothing is executed; the endpoints are only built.
/// </summary>
public class MinimalApiMetadataTests
{
    private sealed record Item(int Id);

    private static readonly WebApplication App = WebApplication.CreateBuilder().Build();
    private static int endpointCounter;

    private static string[] Describe(Delegate handler)
    {
        var pattern = "/metadata-" + Interlocked.Increment(ref endpointCounter);
        App.MapGet(pattern, handler);

        var endpoint = ((IEndpointRouteBuilder)App).DataSources
            .SelectMany(source => source.Endpoints)
            .Cast<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == pattern);

        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Select(m => $"{m.StatusCode}|{m.Type}|{string.Join(",", m.ContentTypes)}")
            .ToArray();
    }

    private static void AssertSameAsNative(Delegate ours, Delegate native, bool expectNonEmpty = true)
    {
        var expected = Describe(native);
        var actual = Describe(ours);

        if (expectNonEmpty)
            Assert.NotEmpty(expected);

        Assert.Equal(expected, actual);
    }

    private static async IAsyncEnumerable<T> Stream<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static readonly Result Success = Result.Success();
    private static readonly Result<Item> Value = Result.Success(new Item(1));

    [Fact]
    public void Ok_MatchesNative()
    {
        AssertSameAsNative(() => Success.ToOk(), () => TypedResults.Ok());
        AssertSameAsNative(() => Value.ToOk(), () => TypedResults.Ok(new Item(1)));
    }

    [Fact]
    public void NoContent_MatchesNative() =>
        AssertSameAsNative(() => Success.ToNoContent(), () => TypedResults.NoContent());

    [Fact]
    public void CreatedAtRoute_MatchesNative()
    {
        AssertSameAsNative(() => Success.ToCreated("n"), () => TypedResults.CreatedAtRoute(routeName: "n"));
        AssertSameAsNative(() => Value.ToCreated("n"), () => TypedResults.CreatedAtRoute(new Item(1), routeName: "n"));
    }

    [Fact]
    public void CreatedAtLocation_MatchesNative()
    {
        AssertSameAsNative(() => Success.ToCreated(), () => TypedResults.Created());
        AssertSameAsNative(() => Success.ToCreatedAtLocation("/x"), () => TypedResults.Created("/x"));
        AssertSameAsNative(() => Value.ToCreatedAtLocation("/x"), () => TypedResults.Created("/x", new Item(1)));
        AssertSameAsNative(
            () => Value.ToCreatedAtLocation(i => $"/x/{i.Id}"),
            () => TypedResults.Created("/x", new Item(1)));
    }

    [Fact]
    public void Accepted_MatchesNative()
    {
        AssertSameAsNative(() => Success.ToAccepted("/x"), () => TypedResults.Accepted("/x"));
        AssertSameAsNative(() => Success.ToAccepted(new Uri("/x", UriKind.Relative)), () => TypedResults.Accepted("/x"));
        AssertSameAsNative(() => Value.ToAccepted("/x"), () => TypedResults.Accepted("/x", new Item(1)));
    }

    [Fact]
    public void AcceptedAtRoute_MatchesNative()
    {
        AssertSameAsNative(() => Success.ToAcceptedAtRoute("n"), () => TypedResults.AcceptedAtRoute(routeName: "n"));
        AssertSameAsNative(
            () => Value.ToAcceptedAtRoute("n"),
            () => TypedResults.AcceptedAtRoute(new Item(1), routeName: "n"));
    }

    [Fact]
    public void Json_MatchesNative() =>
        // Neither the native JsonHttpResult<T> nor ours contributes any produces-metadata today; asserting equality
        // (rather than non-empty) makes this test notice if ASP.NET Core starts adding some.
        AssertSameAsNative(() => Value.ToJson(), () => TypedResults.Json(new Item(1)), expectNonEmpty: false);

    [Fact]
    public void ServerSentEvents_MatchesNative()
    {
        AssertSameAsNative(
            () => Result.Success(Stream<string>()).ToServerSentEvents(),
            () => TypedResults.ServerSentEvents(Stream<string>()));
        AssertSameAsNative(
            () => Result.Success(Stream<Item>()).ToServerSentEvents(),
            () => TypedResults.ServerSentEvents(Stream<Item>()));
        AssertSameAsNative(
            () => Result.Success(Stream<SseItem<int>>()).ToServerSentEvents(),
            () => TypedResults.ServerSentEvents(Stream<SseItem<int>>()));
    }
}

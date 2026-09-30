using System.Net.ServerSentEvents;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace Davish.Result;

/// <summary>
/// The <c>text/event-stream</c> shape returned by <see cref="ResultToMinimalResultExtension.ToServerSentEvents(Result{IAsyncEnumerable{string}}, string?)"/>
/// and <see cref="ResultToMinimalResultExtension.ToServerSentEvents{T}(Result{IAsyncEnumerable{T}}, string?)"/>.
/// </summary>
public sealed class MinimalApiServerSentEventsResult<T> : MinimalApiResult<IAsyncEnumerable<T>>, IEndpointMetadataProvider
{
    private readonly string? eventType;

    internal MinimalApiServerSentEventsResult(Result<IAsyncEnumerable<T>> result, string? eventType) : base(result) =>
        this.eventType = eventType;

    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status200OK;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(IAsyncEnumerable<T> value) =>
        TypedResults.ServerSentEvents(value, eventType);

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<ServerSentEventsResult<T>>(method, builder);
}

/// <summary>
/// The <c>text/event-stream</c> shape returned by
/// <see cref="ResultToMinimalResultExtension.ToServerSentEvents{T}(Result{IAsyncEnumerable{SseItem{T}}})"/>, where
/// each item carries its own event type, id and retry interval.
/// </summary>
public sealed class MinimalApiSseItemResult<T> : MinimalApiResult<IAsyncEnumerable<SseItem<T>>>, IEndpointMetadataProvider
{
    internal MinimalApiSseItemResult(Result<IAsyncEnumerable<SseItem<T>>> result) : base(result)
    {
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status200OK;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(IAsyncEnumerable<SseItem<T>> value) =>
        TypedResults.ServerSentEvents(value);

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<ServerSentEventsResult<T>>(method, builder);
}

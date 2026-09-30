using System.IO.Pipelines;
using System.Net.ServerSentEvents;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Davish.Result;

/// <summary>
/// Converts a <see cref="Result"/> or <see cref="Result{TValue}"/> into a Minimal API result. Success is written
/// immediately; failure is deferred to the registered <see cref="IMinimalApiFailureHandler"/> at execution time
/// (see <see cref="MinimalApiResult"/>/<see cref="MinimalApiResult{TValue}"/>), which requires
/// <c>services.AddResultAspNetCore(o => o.AddMinimalApiResult(...))</c> to be registered.
/// </summary>
public static class ResultToMinimalResultExtension
{
    /// <summary>Converts a successful result to <c>200 OK</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiOkResult ToOk(this Result result) => new(result);

    /// <summary>Converts a successful result to <c>204 No Content</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiNoContentResult ToNoContent(this Result result) => new(result);

    /// <summary>Converts a successful result to <c>201 Created</c> at the given route, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedResult ToCreated(this Result result, string routeName, object? routeValues = null) =>
        new(result, routeName, routeValues);

    /// <summary>Converts a successful result to <c>202 Accepted</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAcceptedResult ToAccepted(this Result result, string? uri = null) => new(result, uri);

    /// <summary>Converts a successful result to <c>200 OK</c> with its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiOkResult<T> ToOk<T>(this Result<T> result) where T : notnull => new(result);

    /// <summary>Converts a successful result to <c>204 No Content</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiNoContentResult ToNoContent<T>(this Result<T> result) where T : notnull => new(result);

    /// <summary>Converts a successful result to <c>201 Created</c> at the given route with its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedResult<T> ToCreated<T>(this Result<T> result, string routeName, Func<T, object?>? routeValues = null)
        where T : notnull =>
        new(result, routeName, routeValues);

    /// <summary>Converts a successful result to <c>202 Accepted</c> with its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAcceptedResult<T> ToAccepted<T>(this Result<T> result, string? uri = null) where T : notnull =>
        new(result, uri);

    // ---- 201 Created (explicit Location) ----

    /// <summary>Converts a successful result to <c>201 Created</c> without a <c>Location</c> header, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedAtLocationResult ToCreated(this Result result) => new(result, (string?)null);

    /// <summary>
    /// Converts a successful result to <c>201 Created</c> with the given <c>Location</c>, or defers the error to
    /// <see cref="IMinimalApiFailureHandler"/>. Named differently from <c>ToCreated(routeName, ...)</c> because a
    /// <see cref="string"/> overload would be indistinguishable from it.
    /// </summary>
    public static MinimalApiCreatedAtLocationResult ToCreatedAtLocation(this Result result, string location) =>
        new(result, location);

    /// <summary>Converts a successful result to <c>201 Created</c> with the given <c>Location</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedAtLocationResult ToCreatedAtLocation(this Result result, Uri location) =>
        new(result, location);

    /// <summary>Converts a successful result to <c>201 Created</c> at the given route, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedResult ToCreated(this Result result, string routeName, RouteValueDictionary routeValues) =>
        new(result, routeName, routeValues);

    /// <summary>Converts a successful result to <c>201 Created</c> with its value and the given <c>Location</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedAtLocationResult<T> ToCreatedAtLocation<T>(this Result<T> result, string location)
        where T : notnull =>
        new(result, location);

    /// <summary>Converts a successful result to <c>201 Created</c> with its value and the given <c>Location</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedAtLocationResult<T> ToCreatedAtLocation<T>(this Result<T> result, Uri location)
        where T : notnull =>
        new(result, location);

    /// <summary>Converts a successful result to <c>201 Created</c> with its value and a <c>Location</c> built from it, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedAtLocationResult<T> ToCreatedAtLocation<T>(this Result<T> result, Func<T, string> location)
        where T : notnull =>
        new(result, location);

    /// <summary>Converts a successful result to <c>201 Created</c> with its value and a <c>Location</c> built from it, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiCreatedAtLocationResult<T> ToCreatedAtLocation<T>(this Result<T> result, Func<T, Uri> location)
        where T : notnull =>
        new(result, location);

    // ---- 202 Accepted ----

    /// <summary>Converts a successful result to <c>202 Accepted</c> with the given <c>Location</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAcceptedResult ToAccepted(this Result result, Uri uri) => new(result, uri);

    /// <summary>Converts a successful result to <c>202 Accepted</c> with its value and the given <c>Location</c>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAcceptedResult<T> ToAccepted<T>(this Result<T> result, Uri uri) where T : notnull =>
        new(result, uri);

    /// <summary>Converts a successful result to <c>202 Accepted</c> at the given route, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAcceptedAtRouteResult ToAcceptedAtRoute(this Result result, string? routeName = null, object? routeValues = null) =>
        new(result, routeName, routeValues);

    /// <summary>Converts a successful result to <c>202 Accepted</c> at the given route, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAcceptedAtRouteResult ToAcceptedAtRoute(this Result result, string? routeName, RouteValueDictionary routeValues) =>
        new(result, routeName, routeValues);

    /// <summary>Converts a successful result to <c>202 Accepted</c> with its value at the given route, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAcceptedAtRouteResult<T> ToAcceptedAtRoute<T>(this Result<T> result, string? routeName = null, Func<T, object?>? routeValues = null)
        where T : notnull =>
        new(result, routeName, routeValues);

    /// <summary>Converts a successful result to <c>202 Accepted</c> with its value at the given route, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAcceptedAtRouteResult<T> ToAcceptedAtRoute<T>(this Result<T> result, string? routeName, RouteValueDictionary routeValues)
        where T : notnull =>
        new(result, routeName, _ => routeValues);

    // ---- Text / Content ----

    /// <summary>Converts a successful result to a text response with fixed content, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiContentResult ToText(this Result result, string content, string? contentType = null, Encoding? contentEncoding = null, int? statusCode = null) =>
        new(result, () => TypedResults.Text(content, contentType, contentEncoding, statusCode), statusCode);

    /// <summary>Converts a successful result to a UTF-8 text response with fixed content, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiContentResult ToText(this Result result, ReadOnlyMemory<byte> utf8Content, string? contentType = null, int? statusCode = null) =>
        new(result, () => TypedResults.Text(utf8Content.Span, contentType, statusCode), statusCode);

    /// <summary>Converts a successful result to a text response built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiContentResult<T> ToText<T>(this Result<T> result, Func<T, string> content, string? contentType = null, Encoding? contentEncoding = null, int? statusCode = null)
        where T : notnull =>
        new(result, value => TypedResults.Text(content(value), contentType, contentEncoding, statusCode), statusCode);

    /// <summary>Converts a successful result to a UTF-8 text response built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiContentResult<T> ToText<T>(this Result<T> result, Func<T, ReadOnlyMemory<byte>> utf8Content, string? contentType = null, int? statusCode = null)
        where T : notnull =>
        new(result, value => TypedResults.Text(utf8Content(value).Span, contentType, statusCode), statusCode);

    /// <summary>Converts a successful result to a content response with fixed content, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiContentResult ToContent(this Result result, string content, string? contentType = null, Encoding? contentEncoding = null, int? statusCode = null) =>
        new(result, () => TypedResults.Content(content, contentType, contentEncoding, statusCode), statusCode);

    /// <summary>Converts a successful result to a content response with fixed content, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiContentResult ToContent(this Result result, string content, MediaTypeHeaderValue contentType) =>
        new(result, () => TypedResults.Content(content, contentType), null);

    /// <summary>Converts a successful result to a content response built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiContentResult<T> ToContent<T>(this Result<T> result, Func<T, string> content, string? contentType = null, Encoding? contentEncoding = null, int? statusCode = null)
        where T : notnull =>
        new(result, value => TypedResults.Content(content(value), contentType, contentEncoding, statusCode), statusCode);

    /// <summary>Converts a successful result to a content response built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiContentResult<T> ToContent<T>(this Result<T> result, Func<T, string> content, MediaTypeHeaderValue contentType)
        where T : notnull =>
        new(result, value => TypedResults.Content(content(value), contentType), null);

    // ---- JSON ----

    /// <summary>Converts a successful result to a JSON response of its value, serialized with <paramref name="options"/>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiJsonResult<T> ToJson<T>(this Result<T> result, JsonSerializerOptions? options = null, string? contentType = null, int? statusCode = null)
        where T : notnull =>
        new(result, value => TypedResults.Json(value, options, contentType, statusCode), statusCode);

    /// <summary>Converts a successful result to a JSON response of its value, serialized with <paramref name="context"/>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiJsonResult<T> ToJson<T>(this Result<T> result, JsonSerializerContext context, string? contentType = null, int? statusCode = null)
        where T : notnull =>
        new(result, value => TypedResults.Json(value, context, contentType, statusCode), statusCode);

    /// <summary>Converts a successful result to a JSON response of its value, serialized with <paramref name="jsonTypeInfo"/>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiJsonResult<T> ToJson<T>(this Result<T> result, JsonTypeInfo<T> jsonTypeInfo, string? contentType = null, int? statusCode = null)
        where T : notnull =>
        new(result, value => TypedResults.Json(value, jsonTypeInfo, contentType, statusCode), statusCode);

    // ---- Server-Sent Events ----

    /// <summary>Converts a successful result holding a stream of strings to a <c>text/event-stream</c> response, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiServerSentEventsResult<string> ToServerSentEvents(this Result<IAsyncEnumerable<string>> result, string? eventType = null) =>
        new(result, eventType);

    /// <summary>Converts a successful result holding a stream of items to a <c>text/event-stream</c> response, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiServerSentEventsResult<T> ToServerSentEvents<T>(this Result<IAsyncEnumerable<T>> result, string? eventType = null) =>
        new(result, eventType);

    /// <summary>Converts a successful result holding a stream of <see cref="SseItem{T}"/> to a <c>text/event-stream</c> response, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiSseItemResult<T> ToServerSentEvents<T>(this Result<IAsyncEnumerable<SseItem<T>>> result) =>
        new(result);

    // ---- Files and streams ----

    /// <summary>Converts a successful result to a file response of fixed bytes, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult ToBytes(this Result result, byte[] contents, string? contentType = null, string? fileDownloadName = null, bool enableRangeProcessing = false, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null) =>
        new(result, () => TypedResults.Bytes(contents, contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a file response of fixed bytes, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult ToBytes(this Result result, ReadOnlyMemory<byte> contents, string? contentType = null, string? fileDownloadName = null, bool enableRangeProcessing = false, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null) =>
        new(result, () => TypedResults.Bytes(contents, contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a file response of bytes built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult<T> ToBytes<T>(this Result<T> result, Func<T, byte[]> contents, string? contentType = null, string? fileDownloadName = null, bool enableRangeProcessing = false, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null)
        where T : notnull =>
        new(result, value => TypedResults.Bytes(contents(value), contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a file response of bytes built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult<T> ToBytes<T>(this Result<T> result, Func<T, ReadOnlyMemory<byte>> contents, string? contentType = null, string? fileDownloadName = null, bool enableRangeProcessing = false, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null)
        where T : notnull =>
        new(result, value => TypedResults.Bytes(contents(value), contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a file response of fixed bytes, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult ToFile(this Result result, byte[] fileContents, string? contentType = null, string? fileDownloadName = null, bool enableRangeProcessing = false, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null) =>
        new(result, () => TypedResults.File(fileContents, contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a file response from a fixed stream, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult ToFile(this Result result, Stream fileStream, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null, bool enableRangeProcessing = false) =>
        new(result, () => TypedResults.File(fileStream, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a file response of bytes built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult<T> ToFile<T>(this Result<T> result, Func<T, byte[]> fileContents, string? contentType = null, string? fileDownloadName = null, bool enableRangeProcessing = false, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null)
        where T : notnull =>
        new(result, value => TypedResults.File(fileContents(value), contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a file response from a stream built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult<T> ToFile<T>(this Result<T> result, Func<T, Stream> fileStream, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null, bool enableRangeProcessing = false)
        where T : notnull =>
        new(result, value => TypedResults.File(fileStream(value), contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a response streaming a file from disk, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult ToPhysicalFile(this Result result, string path, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null, bool enableRangeProcessing = false) =>
        new(result, () => TypedResults.PhysicalFile(path, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a response streaming a file from disk whose path is built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult<T> ToPhysicalFile<T>(this Result<T> result, Func<T, string> path, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null, bool enableRangeProcessing = false)
        where T : notnull =>
        new(result, value => TypedResults.PhysicalFile(path(value), contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a response streaming a file from the web root's file provider, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult ToVirtualFile(this Result result, string path, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null, bool enableRangeProcessing = false) =>
        new(result, () => TypedResults.VirtualFile(path, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a response streaming a file from the web root's file provider whose path is built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult<T> ToVirtualFile<T>(this Result<T> result, Func<T, string> path, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null, bool enableRangeProcessing = false)
        where T : notnull =>
        new(result, value => TypedResults.VirtualFile(path(value), contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a response streaming a fixed <see cref="Stream"/>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult ToStream(this Result result, Stream stream, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null, bool enableRangeProcessing = false) =>
        new(result, () => TypedResults.Stream(stream, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a response streaming a fixed <see cref="PipeReader"/>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult ToStream(this Result result, PipeReader pipeReader, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null, bool enableRangeProcessing = false) =>
        new(result, () => TypedResults.Stream(pipeReader, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a response written by <paramref name="streamWriterCallback"/>, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult ToStream(this Result result, Func<Stream, Task> streamWriterCallback, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null) =>
        new(result, () => TypedResults.Stream(streamWriterCallback, contentType, fileDownloadName, lastModified, entityTag), FileStatusCode(false, lastModified, entityTag));

    /// <summary>Converts a successful result to a response streaming a <see cref="Stream"/> built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult<T> ToStream<T>(this Result<T> result, Func<T, Stream> stream, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null, bool enableRangeProcessing = false)
        where T : notnull =>
        new(result, value => TypedResults.Stream(stream(value), contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a response streaming a <see cref="PipeReader"/> built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult<T> ToStream<T>(this Result<T> result, Func<T, PipeReader> pipeReader, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null, bool enableRangeProcessing = false)
        where T : notnull =>
        new(result, value => TypedResults.Stream(pipeReader(value), contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), FileStatusCode(enableRangeProcessing, lastModified, entityTag));

    /// <summary>Converts a successful result to a response written by a callback that receives its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiFileResult<T> ToStream<T>(this Result<T> result, Func<T, Stream, Task> streamWriterCallback, string? contentType = null, string? fileDownloadName = null, DateTimeOffset? lastModified = null, EntityTagHeaderValue? entityTag = null)
        where T : notnull =>
        new(result, value => TypedResults.Stream(body => streamWriterCallback(value, body), contentType, fileDownloadName, lastModified, entityTag), FileStatusCode(false, lastModified, entityTag));

    // ---- Redirects ----

    /// <summary>Converts a successful result to a redirect to a fixed URL, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiRedirectResult ToRedirect(this Result result, string url, bool permanent = false, bool preserveMethod = false) =>
        new(result, () => TypedResults.Redirect(url, permanent, preserveMethod), RedirectStatusCode(permanent, preserveMethod));

    /// <summary>Converts a successful result to a redirect to a URL built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiRedirectResult<T> ToRedirect<T>(this Result<T> result, Func<T, string> url, bool permanent = false, bool preserveMethod = false)
        where T : notnull =>
        new(result, value => TypedResults.Redirect(url(value), permanent, preserveMethod), RedirectStatusCode(permanent, preserveMethod));

    /// <summary>Converts a successful result to a redirect to a fixed local URL, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiRedirectResult ToLocalRedirect(this Result result, string localUrl, bool permanent = false, bool preserveMethod = false) =>
        new(result, () => TypedResults.LocalRedirect(localUrl, permanent, preserveMethod), RedirectStatusCode(permanent, preserveMethod));

    /// <summary>Converts a successful result to a redirect to a local URL built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiRedirectResult<T> ToLocalRedirect<T>(this Result<T> result, Func<T, string> localUrl, bool permanent = false, bool preserveMethod = false)
        where T : notnull =>
        new(result, value => TypedResults.LocalRedirect(localUrl(value), permanent, preserveMethod), RedirectStatusCode(permanent, preserveMethod));

    /// <summary>Converts a successful result to a redirect to a named route, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiRedirectResult ToRedirectToRoute(this Result result, string? routeName = null, object? routeValues = null, bool permanent = false, bool preserveMethod = false, string? fragment = null) =>
        new(result, () => TypedResults.RedirectToRoute(routeName, routeValues, permanent, preserveMethod, fragment), RedirectStatusCode(permanent, preserveMethod));

    /// <summary>Converts a successful result to a redirect to a named route, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiRedirectResult ToRedirectToRoute(this Result result, string routeName, RouteValueDictionary routeValues, bool permanent = false, bool preserveMethod = false, string? fragment = null) =>
        new(result, () => TypedResults.RedirectToRoute(routeName, routeValues, permanent, preserveMethod, fragment), RedirectStatusCode(permanent, preserveMethod));

    /// <summary>Converts a successful result to a redirect to a named route with route values built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiRedirectResult<T> ToRedirectToRoute<T>(this Result<T> result, string? routeName = null, Func<T, object?>? routeValues = null, bool permanent = false, bool preserveMethod = false, string? fragment = null)
        where T : notnull =>
        new(result, value => TypedResults.RedirectToRoute(routeName, routeValues?.Invoke(value), permanent, preserveMethod, fragment), RedirectStatusCode(permanent, preserveMethod));

    /// <summary>Converts a successful result to a redirect to a named route with fixed route values, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiRedirectResult<T> ToRedirectToRoute<T>(this Result<T> result, string routeName, RouteValueDictionary routeValues, bool permanent = false, bool preserveMethod = false, string? fragment = null)
        where T : notnull =>
        new(result, _ => TypedResults.RedirectToRoute(routeName, routeValues, permanent, preserveMethod, fragment), RedirectStatusCode(permanent, preserveMethod));

    // A file/stream response is only certain to be 200 without range or conditional (ETag/Last-Modified) processing,
    // which can turn it into 206, 304 or 412 at execution time.
    private static int? FileStatusCode(bool enableRangeProcessing, DateTimeOffset? lastModified, EntityTagHeaderValue? entityTag) =>
        enableRangeProcessing || lastModified is not null || entityTag is not null ? null : StatusCodes.Status200OK;

    private static int RedirectStatusCode(bool permanent, bool preserveMethod) =>
        (permanent, preserveMethod) switch
        {
            (true, true) => StatusCodes.Status308PermanentRedirect,
            (true, false) => StatusCodes.Status301MovedPermanently,
            (false, true) => StatusCodes.Status307TemporaryRedirect,
            _ => StatusCodes.Status302Found,
        };

    // ---- Authentication ----

    /// <summary>Converts a successful result to signing in a fixed principal, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAuthenticationResult ToSignIn(this Result result, ClaimsPrincipal principal, AuthenticationProperties? properties = null, string? authenticationScheme = null) =>
        new(result, () => TypedResults.SignIn(principal, properties, authenticationScheme), null);

    /// <summary>Converts a successful result to signing in a principal built from its value, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAuthenticationResult<T> ToSignIn<T>(this Result<T> result, Func<T, ClaimsPrincipal> principal, AuthenticationProperties? properties = null, string? authenticationScheme = null)
        where T : notnull =>
        new(result, value => TypedResults.SignIn(principal(value), properties, authenticationScheme), null);

    /// <summary>Converts a successful result to signing out, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiAuthenticationResult ToSignOut(this Result result, AuthenticationProperties? properties = null, IList<string>? authenticationSchemes = null) =>
        new(result, () => TypedResults.SignOut(properties, authenticationSchemes), null);

    /// <summary>
    /// Converts a successful result to challenging the given authentication schemes (e.g. redirecting to an external
    /// login page), or defers the error to <see cref="IMinimalApiFailureHandler"/>. Unlike a failed result mapped from
    /// <see cref="ErrorType.Unauthorized"/> (<c>401</c> + <c>ProblemDetails</c>), the response is whatever each
    /// authentication handler produces.
    /// </summary>
    public static MinimalApiAuthenticationResult ToChallenge(this Result result, AuthenticationProperties? properties = null, IList<string>? authenticationSchemes = null) =>
        new(result, () => TypedResults.Challenge(properties, authenticationSchemes), null);

    /// <summary>
    /// Converts a successful result to forbidding the given authentication schemes (the caller is authenticated but not
    /// allowed), or defers the error to <see cref="IMinimalApiFailureHandler"/>. The response is whatever each
    /// authentication handler produces.
    /// </summary>
    public static MinimalApiAuthenticationResult ToForbid(this Result result, AuthenticationProperties? properties = null, IList<string>? authenticationSchemes = null) =>
        new(result, () => TypedResults.Forbid(properties, authenticationSchemes), null);

    // ---- Status code ----

    /// <summary>Converts a successful result to a bare response with the given status code, or defers the error to <see cref="IMinimalApiFailureHandler"/>.</summary>
    public static MinimalApiStatusCodeResult ToStatus(this Result result, int statusCode) => new(result, statusCode);
}

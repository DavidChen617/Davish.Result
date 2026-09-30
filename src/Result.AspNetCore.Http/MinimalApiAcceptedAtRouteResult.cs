using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace Davish.Result;

/// <summary>The <c>202 Accepted</c> shape returned by <see cref="ResultToMinimalResultExtension.ToAcceptedAtRoute(Result, string?, object?)"/>.</summary>
/// <param name="result">The result to convert.</param>
/// <param name="routeName">The route whose URL is used for the <c>Location</c> header, if any.</param>
/// <param name="routeValues">The route values for <paramref name="routeName"/>.</param>
public sealed class MinimalApiAcceptedAtRouteResult(Result result, string? routeName, object? routeValues)
    : MinimalApiResult(result), IEndpointMetadataProvider
{
    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status202Accepted;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => TypedResults.AcceptedAtRoute(routeName, routeValues);

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<AcceptedAtRoute>(method, builder);
}

/// <summary>The <c>202 Accepted</c> shape returned by <see cref="ResultToMinimalResultExtension.ToAcceptedAtRoute{T}(Result{T}, string?, Func{T, object?}?)"/>.</summary>
/// <param name="result">The result to convert.</param>
/// <param name="routeName">The route whose URL is used for the <c>Location</c> header, if any.</param>
/// <param name="routeValues">Builds the route values for <paramref name="routeName"/> from the success value.</param>
public sealed class MinimalApiAcceptedAtRouteResult<TValue>(Result<TValue> result, string? routeName, Func<TValue, object?>? routeValues)
    : MinimalApiResult<TValue>(result), IEndpointMetadataProvider
    where TValue : notnull
{
    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status202Accepted;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(TValue value) =>
        TypedResults.AcceptedAtRoute(value, routeName, routeValues?.Invoke(value));

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<AcceptedAtRoute<TValue>>(method, builder);
}

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace Davish.Result;

/// <summary>The <c>201 Created</c> shape returned by <see cref="ResultToMinimalResultExtension.ToCreated(Result, string, object?)"/>.</summary>
/// <param name="result">The result to convert.</param>
/// <param name="routeName">The route whose URL is used for the <c>Location</c> header.</param>
/// <param name="routeValues">The route values for <paramref name="routeName"/>.</param>
public sealed class MinimalApiCreatedResult(Result result, string routeName, object? routeValues)
    : MinimalApiResult(result), IEndpointMetadataProvider
{
    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status201Created;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => TypedResults.CreatedAtRoute(routeName, routeValues);

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<CreatedAtRoute>(method, builder);
}

/// <summary>The <c>201 Created</c> shape returned by <see cref="ResultToMinimalResultExtension.ToCreated{T}(Result{T}, string, Func{T, object?}?)"/>.</summary>
/// <param name="result">The result to convert.</param>
/// <param name="routeName">The route whose URL is used for the <c>Location</c> header.</param>
/// <param name="routeValues">Builds the route values for <paramref name="routeName"/> from the success value.</param>
public sealed class MinimalApiCreatedResult<TValue>(Result<TValue> result, string routeName, Func<TValue, object?>? routeValues)
    : MinimalApiResult<TValue>(result), IEndpointMetadataProvider
    where TValue : notnull
{
    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status201Created;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(TValue value) =>
        TypedResults.CreatedAtRoute(value, routeName, routeValues?.Invoke(value));

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<CreatedAtRoute<TValue>>(method, builder);
}

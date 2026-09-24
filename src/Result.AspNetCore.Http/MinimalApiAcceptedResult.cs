using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace Davish.Result;

/// <summary>The <c>202 Accepted</c> shape returned by <see cref="ResultToMinimalResultExtension.ToAccepted(Result, string?)"/>.</summary>
/// <param name="result">The result to convert.</param>
/// <param name="uri">The URI used for the <c>Location</c> header, if any.</param>
public sealed class MinimalApiAcceptedResult(Result result, string? uri)
    : MinimalApiResult(result), IEndpointMetadataProvider
{
    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status202Accepted;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => TypedResults.Accepted(uri);

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<Accepted>(method, builder);
}

/// <summary>The <c>202 Accepted</c> shape returned by <see cref="ResultToMinimalResultExtension.ToAccepted{T}(Result{T}, string?)"/>.</summary>
/// <param name="result">The result to convert.</param>
/// <param name="uri">The URI used for the <c>Location</c> header, if any.</param>
public sealed class MinimalApiAcceptedResult<TValue>(Result<TValue> result, string? uri)
    : MinimalApiResult<TValue>(result), IEndpointMetadataProvider
    where TValue : notnull
{
    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status202Accepted;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(TValue value) => TypedResults.Accepted(uri, value);

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<Accepted<TValue>>(method, builder);
}

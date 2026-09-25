using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace Davish.Result;

/// <summary>The <c>200 OK</c> shape returned by <see cref="ResultToMinimalResultExtension.ToOk(Result)"/>.</summary>
/// <param name="result">The result to convert.</param>
public sealed class MinimalApiOkResult(Result result)
    : MinimalApiResult(result), IEndpointMetadataProvider
{
    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status200OK;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => TypedResults.Ok();

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<Ok>(method, builder);
}

/// <summary>The <c>200 OK</c> shape returned by <see cref="ResultToMinimalResultExtension.ToOk{T}(Result{T})"/>.</summary>
/// <param name="result">The result to convert.</param>
public sealed class MinimalApiOkResult<TValue>(Result<TValue> result)
    : MinimalApiResult<TValue>(result), IEndpointMetadataProvider
    where TValue : notnull
{
    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status200OK;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(TValue value) => TypedResults.Ok(value);

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<Ok<TValue>>(method, builder);
}

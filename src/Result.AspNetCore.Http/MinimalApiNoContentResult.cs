using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace Davish.Result;

/// <summary>
/// The <c>204 No Content</c> shape returned by both <see cref="ResultToMinimalResultExtension.ToNoContent(Result)"/>
/// and <see cref="ResultToMinimalResultExtension.ToNoContent{T}(Result{T})"/> — a <c>204</c> response never has a
/// body, so there is no generic/non-generic split for this shape.
/// </summary>
/// <param name="result">The result to convert.</param>
public sealed class MinimalApiNoContentResult(Result result)
    : MinimalApiResult(result), IEndpointMetadataProvider
{
    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status204NoContent;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => TypedResults.NoContent();

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<NoContent>(method, builder);
}

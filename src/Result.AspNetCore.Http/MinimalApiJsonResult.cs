using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace Davish.Result;

/// <summary>
/// The JSON shape returned by the <see cref="ResultToMinimalResultExtension.ToJson{T}(Result{T}, System.Text.Json.JsonSerializerOptions?, string?, int?)"/>
/// overloads, which serialize the success value with an explicit serializer configuration.
/// </summary>
public sealed class MinimalApiJsonResult<TValue> : MinimalApiResult<TValue>, IEndpointMetadataProvider
    where TValue : notnull
{
    private readonly Func<TValue, IResult> factory;
    private readonly int successStatusCode;

    internal MinimalApiJsonResult(Result<TValue> result, Func<TValue, IResult> factory, int? statusCode) : base(result)
    {
        this.factory = factory;
        successStatusCode = statusCode ?? StatusCodes.Status200OK;
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => successStatusCode;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(TValue value) => factory(value);

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<JsonHttpResult<TValue>>(method, builder);
}

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace Davish.Result;

/// <summary>The <c>202 Accepted</c> shape returned by <see cref="ResultToMinimalResultExtension.ToAccepted(Result, string?)"/>.</summary>
public sealed class MinimalApiAcceptedResult : MinimalApiResult, IEndpointMetadataProvider
{
    private readonly Func<IResult> factory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="uri">The URI used for the <c>Location</c> header, if any.</param>
    public MinimalApiAcceptedResult(Result result, string? uri) : base(result) =>
        factory = () => TypedResults.Accepted(uri);

    /// <summary>Initializes a new instance.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="uri">The URI used for the <c>Location</c> header.</param>
    public MinimalApiAcceptedResult(Result result, Uri uri) : base(result)
    {
        ArgumentNullException.ThrowIfNull(uri);
        factory = () => TypedResults.Accepted(uri);
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status202Accepted;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => factory();

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<Accepted>(method, builder);
}

/// <summary>The <c>202 Accepted</c> shape returned by <see cref="ResultToMinimalResultExtension.ToAccepted{T}(Result{T}, string?)"/>.</summary>
public sealed class MinimalApiAcceptedResult<TValue> : MinimalApiResult<TValue>, IEndpointMetadataProvider
    where TValue : notnull
{
    private readonly Func<TValue, IResult> factory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="uri">The URI used for the <c>Location</c> header, if any.</param>
    public MinimalApiAcceptedResult(Result<TValue> result, string? uri) : base(result) =>
        factory = value => TypedResults.Accepted(uri, value);

    /// <summary>Initializes a new instance.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="uri">The URI used for the <c>Location</c> header.</param>
    public MinimalApiAcceptedResult(Result<TValue> result, Uri uri) : base(result)
    {
        ArgumentNullException.ThrowIfNull(uri);
        factory = value => TypedResults.Accepted(uri, value);
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status202Accepted;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(TValue value) => factory(value);

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<Accepted<TValue>>(method, builder);
}

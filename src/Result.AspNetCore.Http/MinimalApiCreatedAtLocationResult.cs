using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace Davish.Result;

/// <summary>
/// The <c>201 Created</c> shape with an explicit <c>Location</c> (or none) returned by
/// <see cref="ResultToMinimalResultExtension.ToCreated(Result)"/> and
/// <see cref="ResultToMinimalResultExtension.ToCreatedAtLocation(Result, string)"/>.
/// </summary>
public sealed class MinimalApiCreatedAtLocationResult : MinimalApiResult, IEndpointMetadataProvider
{
    private readonly Func<IResult> factory;

    internal MinimalApiCreatedAtLocationResult(Result result, string? location) : base(result) =>
        factory = location is null ? static () => TypedResults.Created() : () => TypedResults.Created(location);

    internal MinimalApiCreatedAtLocationResult(Result result, Uri location) : base(result)
    {
        ArgumentNullException.ThrowIfNull(location);
        factory = () => TypedResults.Created(location);
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status201Created;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => factory();

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<Created>(method, builder);
}

/// <summary>
/// The <c>201 Created</c> shape with an explicit <c>Location</c> returned by
/// <see cref="ResultToMinimalResultExtension.ToCreatedAtLocation{T}(Result{T}, string)"/> and its overloads.
/// </summary>
public sealed class MinimalApiCreatedAtLocationResult<TValue> : MinimalApiResult<TValue>, IEndpointMetadataProvider
    where TValue : notnull
{
    private readonly Func<TValue, IResult> factory;

    internal MinimalApiCreatedAtLocationResult(Result<TValue> result, string location) : base(result)
    {
        ArgumentNullException.ThrowIfNull(location);
        factory = value => TypedResults.Created(location, value);
    }

    internal MinimalApiCreatedAtLocationResult(Result<TValue> result, Uri location) : base(result)
    {
        ArgumentNullException.ThrowIfNull(location);
        factory = value => TypedResults.Created(location, value);
    }

    internal MinimalApiCreatedAtLocationResult(Result<TValue> result, Func<TValue, string> location) : base(result)
    {
        ArgumentNullException.ThrowIfNull(location);
        factory = value => TypedResults.Created(location(value), value);
    }

    internal MinimalApiCreatedAtLocationResult(Result<TValue> result, Func<TValue, Uri> location) : base(result)
    {
        ArgumentNullException.ThrowIfNull(location);
        factory = value => TypedResults.Created(location(value), value);
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => StatusCodes.Status201Created;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(TValue value) => factory(value);

    /// <inheritdoc cref="IEndpointMetadataProvider.PopulateMetadata"/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) =>
        MetadataHelper.PopulateFrom<Created<TValue>>(method, builder);
}

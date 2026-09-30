using Microsoft.AspNetCore.Http;

namespace Davish.Result;

/// <summary>
/// The file/stream shape returned by <c>ToBytes</c>, <c>ToFile</c>, <c>ToPhysicalFile</c>, <c>ToVirtualFile</c> and <c>ToStream</c> with fixed content. <see cref="MinimalApiResult.StatusCode"/> is <c>200</c> only when no range or conditional (<c>ETag</c>/<c>Last-Modified</c>) processing is requested, since those can turn the response into <c>206</c>, <c>304</c> or <c>412</c> at execution time; otherwise it is <see langword="null"/> (unknown). Like the corresponding built-in <c>TypedResults</c> types, it carries no endpoint metadata.
/// </summary>
public sealed class MinimalApiFileResult : MinimalApiResult
{
    private readonly Func<IResult> factory;
    private readonly int? successStatusCode;

    internal MinimalApiFileResult(Result result, Func<IResult> factory, int? successStatusCode) : base(result)
    {
        this.factory = factory;
        this.successStatusCode = successStatusCode;
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => successStatusCode ?? StatusCodes.Status200OK;

    /// <inheritdoc/>
    protected override bool SuccessStatusCodeIsKnown => successStatusCode.HasValue;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => factory();
}

/// <summary>
/// The file/stream shape returned by the <c>ToBytes</c>, <c>ToFile</c>, <c>ToPhysicalFile</c>, <c>ToVirtualFile</c> and <c>ToStream</c> overloads that build the content from the success value. <see cref="MinimalApiResult{TValue}.StatusCode"/> is <c>200</c> only when no range or conditional (<c>ETag</c>/<c>Last-Modified</c>) processing is requested, otherwise <see langword="null"/> (unknown). Like the corresponding built-in <c>TypedResults</c> types, it carries no endpoint metadata.
/// </summary>
public sealed class MinimalApiFileResult<TValue> : MinimalApiResult<TValue>
    where TValue : notnull
{
    private readonly Func<TValue, IResult> factory;
    private readonly int? successStatusCode;

    internal MinimalApiFileResult(Result<TValue> result, Func<TValue, IResult> factory, int? successStatusCode) : base(result)
    {
        this.factory = factory;
        this.successStatusCode = successStatusCode;
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => successStatusCode ?? StatusCodes.Status200OK;

    /// <inheritdoc/>
    protected override bool SuccessStatusCodeIsKnown => successStatusCode.HasValue;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(TValue value) => factory(value);
}

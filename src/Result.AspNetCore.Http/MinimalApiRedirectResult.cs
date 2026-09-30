using Microsoft.AspNetCore.Http;

namespace Davish.Result;

/// <summary>
/// The redirect shape returned by <c>ToRedirect</c>, <c>ToLocalRedirect</c> and <c>ToRedirectToRoute</c>. The status code is <c>301</c>/<c>302</c>/<c>307</c>/<c>308</c> depending on <c>permanent</c> and <c>preserveMethod</c>. Like the corresponding built-in <c>TypedResults</c> types, it carries no endpoint metadata.
/// </summary>
public sealed class MinimalApiRedirectResult : MinimalApiResult
{
    private readonly Func<IResult> factory;
    private readonly int successStatusCode;

    internal MinimalApiRedirectResult(Result result, Func<IResult> factory, int successStatusCode) : base(result)
    {
        this.factory = factory;
        this.successStatusCode = successStatusCode;
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => successStatusCode;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => factory();
}

/// <summary>
/// The redirect shape returned by the <c>ToRedirect</c>, <c>ToLocalRedirect</c> and <c>ToRedirectToRoute</c> overloads that build the target from the success value. Like the corresponding built-in <c>TypedResults</c> types, it carries no endpoint metadata.
/// </summary>
public sealed class MinimalApiRedirectResult<TValue> : MinimalApiResult<TValue>
    where TValue : notnull
{
    private readonly Func<TValue, IResult> factory;
    private readonly int successStatusCode;

    internal MinimalApiRedirectResult(Result<TValue> result, Func<TValue, IResult> factory, int successStatusCode) : base(result)
    {
        this.factory = factory;
        this.successStatusCode = successStatusCode;
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => successStatusCode;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(TValue value) => factory(value);
}

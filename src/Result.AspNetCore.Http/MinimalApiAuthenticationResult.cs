using Microsoft.AspNetCore.Http;

namespace Davish.Result;

/// <summary>
/// The authentication shape returned by <c>ToSignIn</c>, <c>ToSignOut</c>, <c>ToChallenge</c> and <c>ToForbid</c>. The actual status code is decided by the authentication handler at execution time, so <see cref="MinimalApiResult.StatusCode"/> is <see langword="null"/> (unknown) on success — for example a cookie handler answers <c>302</c> when a redirect URI is set. Like the corresponding built-in <c>TypedResults</c> types, it carries no endpoint metadata.
/// </summary>
public sealed class MinimalApiAuthenticationResult : MinimalApiResult
{
    private readonly Func<IResult> factory;
    private readonly int? successStatusCode;

    internal MinimalApiAuthenticationResult(Result result, Func<IResult> factory, int? successStatusCode) : base(result)
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
/// The sign-in shape returned by the <c>ToSignIn</c> overload that builds the principal from the success value. The actual status code is decided by the authentication handler at execution time, so <see cref="MinimalApiResult{TValue}.StatusCode"/> is <see langword="null"/> (unknown) on success. Like the corresponding built-in <c>TypedResults</c> types, it carries no endpoint metadata.
/// </summary>
public sealed class MinimalApiAuthenticationResult<TValue> : MinimalApiResult<TValue>
    where TValue : notnull
{
    private readonly Func<TValue, IResult> factory;
    private readonly int? successStatusCode;

    internal MinimalApiAuthenticationResult(Result<TValue> result, Func<TValue, IResult> factory, int? successStatusCode) : base(result)
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

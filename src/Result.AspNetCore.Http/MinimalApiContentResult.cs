using Microsoft.AspNetCore.Http;

namespace Davish.Result;

/// <summary>
/// The text/content shape returned by <see cref="ResultToMinimalResultExtension.ToText(Result, string, string?, System.Text.Encoding?, int?)"/>
/// and <see cref="ResultToMinimalResultExtension.ToContent(Result, string, string?, System.Text.Encoding?, int?)"/> (and their overloads). Like the built-in <c>Results.Text</c>/<c>Results.Content</c>, it carries no endpoint
/// metadata, since the content type is only known at runtime.
/// </summary>
public sealed class MinimalApiContentResult : MinimalApiResult
{
    private readonly Func<IResult> factory;
    private readonly int successStatusCode;

    internal MinimalApiContentResult(Result result, Func<IResult> factory, int? statusCode) : base(result)
    {
        this.factory = factory;
        successStatusCode = statusCode ?? StatusCodes.Status200OK;
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => successStatusCode;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => factory();
}

/// <summary>
/// The text/content shape returned by <see cref="ResultToMinimalResultExtension.ToText{T}(Result{T}, Func{T, string}, string?, System.Text.Encoding?, int?)"/>
/// and <see cref="ResultToMinimalResultExtension.ToContent{T}(Result{T}, Func{T, string}, string?, System.Text.Encoding?, int?)"/> (and their overloads).
/// </summary>
public sealed class MinimalApiContentResult<TValue> : MinimalApiResult<TValue>
    where TValue : notnull
{
    private readonly Func<TValue, IResult> factory;
    private readonly int successStatusCode;

    internal MinimalApiContentResult(Result<TValue> result, Func<TValue, IResult> factory, int? statusCode) : base(result)
    {
        this.factory = factory;
        successStatusCode = statusCode ?? StatusCodes.Status200OK;
    }

    /// <inheritdoc/>
    protected override int SuccessStatusCode => successStatusCode;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult(TValue value) => factory(value);
}

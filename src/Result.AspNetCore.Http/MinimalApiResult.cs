using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Davish.Result;

/// <summary>
/// Base type for converting a non-generic <see cref="Result"/> into a Minimal API <see cref="IResult"/>, deferring
/// failure handling to the registered <see cref="IMinimalApiFailureHandler"/> at execution time. Public so
/// consumers can implement their own success shapes (e.g. <c>206 Partial Content</c>) while reusing the failure
/// handling below; the built-in shapes (<see cref="MinimalApiOkResult"/> etc.) are <see langword="internal"/>.
/// </summary>
public abstract class MinimalApiResult : IResult, IStatusCodeHttpResult
{
    private readonly Result _result;

    /// <summary>Initializes a new instance wrapping <paramref name="result"/>.</summary>
    protected MinimalApiResult(Result result) => _result = result;

    /// <summary>The HTTP status code written on success.</summary>
    protected abstract int SuccessStatusCode { get; }

    /// <summary>
    /// Whether <see cref="SuccessStatusCode"/> is the status code that will actually be written. Override to return
    /// <see langword="false"/> when the real status is only decided at execution time (for example by an
    /// authentication handler, or by range/conditional request processing); <see cref="StatusCode"/> then reports
    /// <see langword="null"/> on success instead of a value that may be wrong. Defaults to <see langword="true"/>.
    /// </summary>
    protected virtual bool SuccessStatusCodeIsKnown => true;

    /// <summary>Builds the <see cref="IResult"/> to execute when the wrapped <see cref="Result"/> is successful.</summary>
    protected abstract IResult CreateSuccessResult();

    /// <inheritdoc/>
    public int? StatusCode => _result.IsSuccess
        ? (SuccessStatusCodeIsKnown ? SuccessStatusCode : null)
        : _result.Error.Type.ToStatusCode();

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var httpResult = _result.IsSuccess
            ? CreateSuccessResult()
            : await ResolveFailureAsync(httpContext);

        await httpResult.ExecuteAsync(httpContext);
    }

    private async Task<IResult> ResolveFailureAsync(HttpContext httpContext)
    {
        var handler = httpContext.RequestServices.GetRequiredService<IMinimalApiFailureHandler>();
        return await handler.HandleAsync(_result, httpContext.RequestAborted);
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Davish.Result;

/// <summary>
/// Base type for converting a <see cref="Result{TValue}"/> into a Minimal API <see cref="IResult"/>, deferring
/// failure handling to the registered <see cref="IMinimalApiFailureHandler"/> at execution time. Public so
/// consumers can implement their own success shapes (e.g. <c>206 Partial Content</c>) while reusing the failure
/// handling below; the built-in shapes (<see cref="MinimalApiOkResult{TValue}"/> etc.) are <see langword="internal"/>.
/// </summary>
public abstract class MinimalApiResult<TValue> : IResult, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<TValue>
    where TValue : notnull
{
    private readonly Result<TValue> result;

    /// <summary>Initializes a new instance wrapping <paramref name="result"/>.</summary>
    protected MinimalApiResult(Result<TValue> result) => this.result = result;

    /// <summary>The HTTP status code written on success.</summary>
    protected abstract int SuccessStatusCode { get; }

    /// <summary>
    /// Whether <see cref="SuccessStatusCode"/> is the status code that will actually be written. Override to return
    /// <see langword="false"/> when the real status is only decided at execution time (for example by an
    /// authentication handler, or by range/conditional request processing); <see cref="StatusCode"/> then reports
    /// <see langword="null"/> on success instead of a value that may be wrong. Defaults to <see langword="true"/>.
    /// </summary>
    protected virtual bool SuccessStatusCodeIsKnown => true;

    /// <summary>Builds the <see cref="IResult"/> to execute when the wrapped <see cref="Result{TValue}"/> is successful.</summary>
    protected abstract IResult CreateSuccessResult(TValue value);

    /// <inheritdoc/>
    public int? StatusCode => result.IsSuccess
        ? (SuccessStatusCodeIsKnown ? SuccessStatusCode : null)
        : result.Error.Type.ToStatusCode();

    /// <summary>
    /// Gets the success value. Throws <see cref="ResultValueUnavailableException"/> on failure, the same as
    /// <see cref="Result{TValue}.Value"/> itself — callers should check <see cref="StatusCode"/> first.
    /// </summary>
    TValue IValueHttpResult<TValue>.Value => result.Value;

    object? IValueHttpResult.Value => result.IsSuccess ? result.Value : null;

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var httpResult = result.IsSuccess
            ? CreateSuccessResult(result.Value)
            : await ResolveFailureAsync(httpContext);

        await httpResult.ExecuteAsync(httpContext);
    }

    private async Task<IResult> ResolveFailureAsync(HttpContext httpContext)
    {
        var handler = httpContext.RequestServices.GetRequiredService<IMinimalApiFailureHandler>();
        return await handler.HandleAsync(result, httpContext.RequestAborted);
    }
}

using Microsoft.AspNetCore.Http;

namespace Davish.Result;

/// <summary>
/// The bare status-code shape returned by <see cref="ResultToMinimalResultExtension.ToStatus(Result, int)"/>. Like the
/// built-in <c>TypedResults.StatusCode</c>, it carries no endpoint metadata.
/// </summary>
/// <param name="result">The result to convert.</param>
/// <param name="statusCode">The status code written on success.</param>
public sealed class MinimalApiStatusCodeResult(Result result, int statusCode) : MinimalApiResult(result)
{
    /// <inheritdoc/>
    protected override int SuccessStatusCode => statusCode;

    /// <inheritdoc/>
    protected override IResult CreateSuccessResult() => TypedResults.StatusCode(statusCode);
}

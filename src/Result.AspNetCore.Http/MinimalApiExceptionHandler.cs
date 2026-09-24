using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Davish.Result;

/// <summary>
/// Translates a caught exception into a <see cref="Result"/> via the registered <see cref="IExceptionMapper{TSource}"/>
/// instances, then converts it into a response using the same <see cref="IMinimalApiFailureHandler"/> used by
/// <see cref="MinimalApiResult"/>/<see cref="MinimalApiResult{TValue}"/>. Registered as an <see cref="IExceptionHandler"/>;
/// requires <c>app.UseExceptionHandler()</c> to actually be invoked.
/// </summary>
internal sealed class MinimalApiExceptionHandler(
    IExceptionMapperDispatcher dispatcher,
    IMinimalApiFailureHandler failureHandler) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var result = await dispatcher.MapAsync(context, exception, cancellationToken);
        if (result is null)
            return false;

        var httpResult = await failureHandler.HandleAsync(result, cancellationToken);
        await httpResult.ExecuteAsync(context);
        return true;
    }
}

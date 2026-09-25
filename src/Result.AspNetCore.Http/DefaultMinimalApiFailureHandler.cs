using Microsoft.AspNetCore.Http;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace Davish.Result;

/// <summary>
/// The default <see cref="IMinimalApiFailureHandler"/>: converts a failed <see cref="Result"/>'s <see cref="Error"/>
/// into a <c>ProblemDetails</c> (or <c>ValidationProblemDetails</c>, if <see cref="Error.Fields"/> is non-empty)
/// response, using <c>ErrorType.ToStatusCode()</c> to resolve the status code.
/// </summary>
internal sealed class DefaultMinimalApiFailureHandler : IMinimalApiFailureHandler
{
    public ValueTask<IResult> HandleAsync(Result result, CancellationToken cancellationToken)
    {
        var error = result.Error;

        IResult httpResult = error.Fields.Count > 0
            ? ValidationProblem(
                title: error.Code,
                detail: error.Description,
                errors: error.Fields.Select(x => new KeyValuePair<string, string[]>(x.Key, [.. x.Value])))
            : Problem(
                title: error.Code,
                detail: error.Description,
                statusCode: error.Type.ToStatusCode());

        return ValueTask.FromResult(httpResult);
    }
}

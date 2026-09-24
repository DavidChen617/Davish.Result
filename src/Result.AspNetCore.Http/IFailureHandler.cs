using Microsoft.AspNetCore.Http;

namespace Davish.Result;

/// <summary>
/// Converts a failed <see cref="Result"/> into a host-specific response type. Only ever called for a
/// <see cref="Result"/> whose <see cref="Result.IsSuccess"/> is <see langword="false"/>.
/// </summary>
/// <typeparam name="TResult">The host-specific response type, e.g. <see cref="IResult"/> for Minimal API.</typeparam>
public interface IFailureHandler<TResult>
{
    /// <summary>Converts <paramref name="result"/>'s error into a <typeparamref name="TResult"/>.</summary>
    /// <param name="result">The failed <see cref="Result"/>.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    ValueTask<TResult> HandleAsync(Result result, CancellationToken cancellationToken);
}

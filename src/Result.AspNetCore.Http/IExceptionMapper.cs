using Microsoft.AspNetCore.Http;

namespace Davish.Result;

/// <summary>
/// Translates a caught <typeparamref name="TSource"/> into a failed <see cref="Result"/>, so it can flow through
/// the same failure pipeline as a <see cref="Result"/> returned directly by application code.
/// </summary>
/// <typeparam name="TSource">The exception type this mapper handles.</typeparam>
public interface IExceptionMapper<in TSource> where TSource : Exception
{
    /// <summary>Maps <paramref name="source"/> into a failed <see cref="Result"/>.</summary>
    /// <param name="context">The current request's <see cref="HttpContext"/>.</param>
    /// <param name="source">The exception to translate.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    ValueTask<Result> MapAsync(HttpContext context, TSource source, CancellationToken cancellationToken);
}

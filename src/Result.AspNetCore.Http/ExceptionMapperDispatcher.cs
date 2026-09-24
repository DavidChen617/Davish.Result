using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace Davish.Result;

/// <summary>
/// Resolves the <see cref="Result"/> for a caught exception by finding the most specific registered
/// <see cref="IExceptionMapper{TSource}"/> along the exception's inheritance chain.
/// </summary>
internal interface IExceptionMapperDispatcher
{
    /// <summary>
    /// Maps <paramref name="exception"/> into a failed <see cref="Result"/> using the most specific registered
    /// mapper, or <see langword="null"/> if no mapper is registered for <paramref name="exception"/> or any of its
    /// base types.
    /// </summary>
    ValueTask<Result?> MapAsync(HttpContext context, Exception exception, CancellationToken cancellationToken);
}

/// <summary>
/// Matches a caught exception's runtime type against registered mappers by walking up its inheritance chain
/// (the same way a <see langword="catch"/> clause matches), most-derived first. Resolutions are cached per
/// concrete exception type so the walk only happens once per type.
/// </summary>
internal sealed class ExceptionMapperDispatcher(
    IReadOnlyDictionary<Type, Func<HttpContext, Exception, CancellationToken, ValueTask<Result>>> mappers)
    : IExceptionMapperDispatcher
{
    private readonly ConcurrentDictionary<Type, Func<HttpContext, Exception, CancellationToken, ValueTask<Result>>?> _cache = new();

    public async ValueTask<Result?> MapAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var mapper = _cache.GetOrAdd(exception.GetType(), Resolve);

        return mapper is null ? null : await mapper(context, exception, cancellationToken);
    }

    private Func<HttpContext, Exception, CancellationToken, ValueTask<Result>>? Resolve(Type type)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            if (mappers.TryGetValue(t, out var mapper))
                return mapper;
        }

        return null;
    }
}

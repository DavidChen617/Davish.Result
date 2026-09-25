using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Davish.Result;

/// <summary>Registers <see cref="Davish.Result"/>'s ASP.NET Core integration.</summary>
public static class ResultAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers exception-to-<see cref="Result"/> mapping and (if configured via
    /// <see cref="ResultAspNetCoreOptions.AddMinimalApiResult"/>) Minimal API failure handling.
    /// Requires <c>app.UseExceptionHandler()</c> and <see cref="ResultAspNetCoreOptions.AddMinimalApiResult"/>
    /// for the exception mapping to actually take effect.
    /// </summary>
    /// <remarks>
    /// Also calls <c>services.AddProblemDetails()</c>. ASP.NET Core's parameterless <c>app.UseExceptionHandler()</c>
    /// throws at startup unless a fallback is configured for exceptions that no
    /// <see cref="Microsoft.AspNetCore.Diagnostics.IExceptionHandler"/> handles (i.e. no <c>MapExceptionToResult</c>
    /// registration matched) — <c>AddProblemDetails()</c> is that fallback, so unmapped exceptions still get a
    /// ProblemDetails response instead of crashing the app at startup. This is host-agnostic (MVC's
    /// <c>[ApiController]</c>/<c>ControllerBase.Problem()</c> use the same <see cref="ProblemDetailsOptions"/>),
    /// unlike <see cref="ResultAspNetCoreOptions.AddMinimalApiResult"/> itself, so it's registered here
    /// rather than nested under it.
    /// </remarks>
    public static IServiceCollection AddResultAspNetCore(
        this IServiceCollection services,
        Action<ResultAspNetCoreOptions> configureOptions)
    {
        var options = new ResultAspNetCoreOptions(services);
        configureOptions(options);

        services.TryAddSingleton(_ => options.BuildDispatcher());
        services.AddProblemDetails();

        return services;
    }
}

/// <summary>Configures <see cref="ResultAspNetCoreServiceCollectionExtensions.AddResultAspNetCore"/>.</summary>
public sealed class ResultAspNetCoreOptions(IServiceCollection services)
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, CancellationToken, ValueTask<Result>>> _mappers = [];

    /// <summary>Maps <typeparamref name="TException"/> into a <see cref="Result"/> using a synchronous delegate.</summary>
    public void MapExceptionToResult<TException>(Func<HttpContext, TException, Result> mapper)
        where TException : Exception
    {
        _mappers[typeof(TException)] = (context, exception, _) =>
            ValueTask.FromResult(mapper(context, (TException)exception));
    }

    /// <summary>Maps <typeparamref name="TException"/> into a <see cref="Result"/> using an asynchronous delegate.</summary>
    public void MapExceptionToResult<TException>(
        Func<HttpContext, TException, CancellationToken, ValueTask<Result>> mapper)
        where TException : Exception
    {
        _mappers[typeof(TException)] = (context, exception, ct) => mapper(context, (TException)exception, ct);
    }

    /// <summary>
    /// Maps <typeparamref name="TException"/> into a <see cref="Result"/> using a DI-resolved
    /// <see cref="IExceptionMapper{TSource}"/>. <typeparamref name="TMapper"/> is registered as transient and
    /// resolved per exception, from the current request's <see cref="HttpContext.RequestServices"/> — not
    /// captured at registration time, to avoid a captive dependency if <typeparamref name="TMapper"/> has scoped
    /// dependencies.
    /// </summary>
    public void MapExceptionToResult<TException, TMapper>()
        where TException : Exception
        where TMapper : class, IExceptionMapper<TException>
    {
        services.TryAddTransient<TMapper>();
        _mappers[typeof(TException)] = (context, exception, ct) =>
        {
            var mapper = context.RequestServices.GetRequiredService<TMapper>();
            return mapper.MapAsync(context, (TException)exception, ct);
        };
    }

    /// <summary>
    /// Registers the Minimal-API-specific integration: the default <see cref="IMinimalApiFailureHandler"/>
    /// (overridable via <see cref="MinimalApiResultOptions.MinimalApiFailureHandler{THandler}"/>), the
    /// <see cref="MinimalApiExceptionHandler"/> that maps caught exceptions through it, and any additional
    /// configuration in <paramref name="configureOptions"/>, if given.
    /// </summary>
    public void AddMinimalApiResult(Action<MinimalApiResultOptions>? configureOptions = null)
    {
        services.TryAddSingleton<IMinimalApiFailureHandler, DefaultMinimalApiFailureHandler>();
        services.AddExceptionHandler<MinimalApiExceptionHandler>();
        configureOptions?.Invoke(new MinimalApiResultOptions(services));
    }

    /// <summary>
    /// Convenience wrapper for <see cref="ResultHttpOptions.Configure"/>, so the <see cref="ErrorType"/> → HTTP
    /// status code mapping can be set from the same call as the rest of the ASP.NET Core integration. This does
    /// not change <see cref="ResultHttpOptions"/>'s own process-wide static, lock-on-first-use model — it just
    /// forwards to it.
    /// </summary>
    public void ConfigureStatusCodes(Action<ResultHttpOptionsBuilder> configure) => ResultHttpOptions.Configure(configure);

    /// <summary>
    /// Configures the <see cref="ProblemDetailsOptions"/> used by the <c>services.AddProblemDetails()</c> call
    /// that <see cref="ResultAspNetCoreServiceCollectionExtensions.AddResultAspNetCore"/> already makes — e.g. to
    /// set <see cref="ProblemDetailsOptions.CustomizeProblemDetails"/>.
    /// </summary>
    public void ConfigureProblemDetails(Action<ProblemDetailsOptions> configure) => services.Configure(configure);

    internal IExceptionMapperDispatcher BuildDispatcher() => new ExceptionMapperDispatcher(_mappers);
}

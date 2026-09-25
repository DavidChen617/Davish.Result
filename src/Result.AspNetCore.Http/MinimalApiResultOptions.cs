using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Davish.Result;

/// <summary>Configures <see cref="ResultAspNetCoreOptions.AddMinimalApiResult"/>.</summary>
public sealed class MinimalApiResultOptions(IServiceCollection services)
{
    /// <summary>
    /// Replaces the registered <see cref="IMinimalApiFailureHandler"/> with <typeparamref name="THandler"/>.
    /// </summary>
    public void MinimalApiFailureHandler<THandler>() where THandler : class, IMinimalApiFailureHandler
    {
        services.Replace(ServiceDescriptor.Singleton<IMinimalApiFailureHandler, THandler>());
    }
}
